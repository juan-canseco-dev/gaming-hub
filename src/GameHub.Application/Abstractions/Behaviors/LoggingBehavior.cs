using GameHub.Application.Abstractions.Messaging;
using System.Diagnostics;
using GameHub.Abstractions.Primitives;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GameHub.Application.Abstractions.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<TRequest> _logger;
    public LoggingBehavior(ILogger<TRequest> logger)
    {
        _logger = logger;
    }
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var startedAt = Stopwatch.GetTimestamp();

        _logger.LogDebug("Handling {RequestName}", requestName);

        try
        {
            var response = await next();
            var elapsedMilliseconds = Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;

            if (response is Result { IsFailure: true } failedResult)
            {
                _logger.LogDebug(
                    "Handled {RequestName} with failure {ErrorCode} in {ElapsedMilliseconds:0.0000} ms",
                    requestName,
                    failedResult.Error.Code,
                    elapsedMilliseconds);
            }
            else
            {
                _logger.LogDebug(
                    "Handled {RequestName} successfully in {ElapsedMilliseconds:0.0000} ms",
                    requestName,
                    elapsedMilliseconds);
            }

            return response;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogDebug(
                "Handling {RequestName} was canceled after {ElapsedMilliseconds:0.0000} ms",
                requestName,
                Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds);
            throw;
        }
    }
}
