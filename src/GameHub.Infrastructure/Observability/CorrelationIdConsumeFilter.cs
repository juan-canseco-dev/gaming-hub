using System.Diagnostics;
using GameHub.Application.Abstractions.Observability;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace GameHub.Infrastructure.Observability;

internal sealed class CorrelationIdConsumeFilter<T> : IFilter<ConsumeContext<T>>
    where T : class
{
    private readonly ICorrelationIdAccessor _correlationIdAccessor;
    private readonly ILogger<CorrelationIdConsumeFilter<T>> _logger;

    public CorrelationIdConsumeFilter(
        ICorrelationIdAccessor correlationIdAccessor,
        ILogger<CorrelationIdConsumeFilter<T>> logger)
    {
        _correlationIdAccessor = correlationIdAccessor;
        _logger = logger;
    }

    public async Task Send(ConsumeContext<T> context, IPipe<ConsumeContext<T>> next)
    {
        var correlationId = ResolveCorrelationId(context);
        var previousCorrelationId = _correlationIdAccessor.CorrelationId;
        _correlationIdAccessor.CorrelationId = correlationId;
        Activity.Current?.SetTag("correlation.id", correlationId);

        var scopeProperties = new Dictionary<string, object>
        {
            [CorrelationIdConstants.LogPropertyName] = correlationId,
            ["MessageType"] = typeof(T).Name
        };

        if (context.MessageId.HasValue)
        {
            scopeProperties["MessageId"] = context.MessageId.Value;
        }

        using var scope = _logger.BeginScope(scopeProperties);

        try
        {
            await next.Send(context);
        }
        finally
        {
            _correlationIdAccessor.CorrelationId = previousCorrelationId;
        }
    }

    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("correlationIdConsume");
    }

    private static string ResolveCorrelationId(ConsumeContext context)
    {
        var headerValue = context.Headers.Get<string>(CorrelationIdConstants.HeaderName);
        if (!string.IsNullOrWhiteSpace(headerValue))
        {
            return headerValue;
        }

        return context.CorrelationId?.ToString("N")
            ?? context.ConversationId?.ToString("N")
            ?? Activity.Current?.TraceId.ToHexString()
            ?? Guid.CreateVersion7().ToString("N");
    }
}
