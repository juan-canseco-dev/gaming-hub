using GameHub.Application.Exceptions;
using GameHub.Application.Abstractions.Observability;
using GameHub.Web.API.ProblemDetails;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameHub.Web.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IProblemDetailsService _problemDetailsService;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IProblemDetailsService problemDetailsService)
    {
        _next = next;
        _logger = logger;
        _problemDetailsService = problemDetailsService;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            if (context.Response.HasStarted)
            {
                _logger.LogError(
                    exception,
                    "Unhandled exception after the response had already started");
                throw;
            }

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
            var problemDetails = CreateProblemDetails(exceptionDetails);

            context.Response.StatusCode = exceptionDetails.Status;
            if (exceptionDetails.RetryAfterSeconds is int retryAfterSeconds)
            {
                context.Response.Headers.RetryAfter = retryAfterSeconds.ToString();
            }

            await _problemDetailsService.WriteAsync(new ProblemDetailsContext
            {
                HttpContext = context,
                ProblemDetails = problemDetails,
                Exception = exception
            });

        }
    }

    private static ExceptionDetails GetExceptionDetails(Exception exception)
    {
        return exception switch
        {
            ValidationException validationException => new ExceptionDetails(
                StatusCodes.Status400BadRequest,
                ProblemTypes.Validation,
                "Validation Error",
                "One or more validation errors occurred.",
                validationException.Errors,
                null
            ),
            SqlException or DbUpdateException { InnerException: SqlException } => new ExceptionDetails(
                StatusCodes.Status503ServiceUnavailable,
                ProblemTypes.DatabaseUnavailable,
                "Service Temporarily Unavailable",
                "The service is temporarily unable to access its data store. Retry later.",
                null,
                30
            ),
            _ => new ExceptionDetails(
                StatusCodes.Status500InternalServerError,
                ProblemTypes.InternalServerError,
                "Internal Server Error",
                "An unexpected error occurred in the app.",
                null,
                null
            )

        };
    }

    private static Microsoft.AspNetCore.Mvc.ProblemDetails CreateProblemDetails(
        ExceptionDetails details)
    {
        if (details.Errors is not null)
        {
            var errors = details.Errors
                .GroupBy(error => error.PropertyName)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(error => error.ErrorMessage).Distinct().ToArray());

            return new ValidationProblemDetails(errors)
            {
                Status = details.Status,
                Type = details.Type,
                Title = details.Title,
                Detail = details.Detail
            };
        }

        return new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Status = details.Status,
            Type = details.Type,
            Title = details.Title,
            Detail = details.Detail
        };
    }

    internal record ExceptionDetails(
      int Status,
      string Type,
      string Title,
      string Detail,
      IEnumerable<ValidationError>? Errors,
      int? RetryAfterSeconds
  );
}
