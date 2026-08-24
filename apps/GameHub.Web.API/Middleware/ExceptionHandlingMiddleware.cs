using GameHub.Application.Exceptions;
using GameHub.Application.Abstractions.Observability;
using Microsoft.AspNetCore.Mvc;

namespace GameHub.Web.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            var exceptionDetails = GetExceptionDetails(exception);

            if (exceptionDetails.Status >= StatusCodes.Status500InternalServerError)
            {
                _logger.LogError(exception, "Unhandled exception while processing the request");
            }
            else
            {
                _logger.LogWarning(
                    "Request rejected with {ErrorType} and status code {StatusCode}",
                    exceptionDetails.Type,
                    exceptionDetails.Status);
            }
            var problemDetails = new ProblemDetails
            {
                Status = exceptionDetails.Status,
                Type = exceptionDetails.Type,
                Title = exceptionDetails.Title,
                Detail = exceptionDetails.Detail
            };

            if (exceptionDetails.Errors is not null)
            {
                problemDetails.Extensions["errors"] = exceptionDetails.Errors;
            }

            problemDetails.Extensions["correlationId"] =
                context.Items[CorrelationIdConstants.LogPropertyName]?.ToString()
                ?? context.TraceIdentifier;

            context.Response.StatusCode = exceptionDetails.Status;

            await context.Response.WriteAsJsonAsync(problemDetails);

        }
    }

    private static ExceptionDetails GetExceptionDetails(Exception exception)
    {
        return exception switch
        {
            ValidationException validationException => new ExceptionDetails(
                StatusCodes.Status400BadRequest,
                "ValidationFailure",
                "Validation Error",
                "One or more validation errors occurred.",
                validationException.Errors
            ),
            _ => new ExceptionDetails(
                StatusCodes.Status500InternalServerError,
                "ServerError",
                "Internal Server Error",
                "An unexpected error occurred in the app.",
                null
            )

        };
    }

    internal record ExceptionDetails(
      int Status,
      string Type,
      string Title,
      string Detail,
      IEnumerable<object>? Errors
  );
}
