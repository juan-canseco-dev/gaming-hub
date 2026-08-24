using System.Diagnostics;
using GameHub.Application.Abstractions.Observability;
using Microsoft.Extensions.Primitives;

namespace GameHub.Web.API.Middleware;

public sealed class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(
        RequestDelegate next,
        ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ICorrelationIdAccessor correlationIdAccessor)
    {
        var correlationId = ResolveCorrelationId(context);
        var previousCorrelationId = correlationIdAccessor.CorrelationId;

        correlationIdAccessor.CorrelationId = correlationId;
        context.TraceIdentifier = correlationId;
        context.Items[CorrelationIdConstants.LogPropertyName] = correlationId;
        context.Response.Headers[CorrelationIdConstants.HeaderName] = correlationId;
        Activity.Current?.SetTag("correlation.id", correlationId);

        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            [CorrelationIdConstants.LogPropertyName] = correlationId
        });

        try
        {
            await _next(context);
        }
        finally
        {
            correlationIdAccessor.CorrelationId = previousCorrelationId;
        }
    }

    private static string ResolveCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(
                CorrelationIdConstants.HeaderName,
                out StringValues headerValues))
        {
            var candidate = headerValues.FirstOrDefault();
            if (IsValid(candidate))
            {
                return candidate!;
            }
        }

        return Activity.Current?.TraceId.ToHexString()
            ?? Guid.CreateVersion7().ToString("N");
    }

    private static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > CorrelationIdConstants.MaxLength)
        {
            return false;
        }

        return value.All(character =>
            char.IsAsciiLetterOrDigit(character) || character is '-' or '_' or '.');
    }
}
