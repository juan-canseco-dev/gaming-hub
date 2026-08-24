using GameHub.Application.Abstractions.Observability;
using MassTransit;

namespace GameHub.Infrastructure.Observability;

internal sealed class CorrelationIdPublishFilter<T> : IFilter<PublishContext<T>>
    where T : class
{
    private readonly ICorrelationIdAccessor _correlationIdAccessor;

    public CorrelationIdPublishFilter(ICorrelationIdAccessor correlationIdAccessor)
    {
        _correlationIdAccessor = correlationIdAccessor;
    }

    public async Task Send(PublishContext<T> context, IPipe<PublishContext<T>> next)
    {
        var correlationId = _correlationIdAccessor.CorrelationId;
        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            context.Headers.Set(CorrelationIdConstants.HeaderName, correlationId);

            if (!context.CorrelationId.HasValue && Guid.TryParse(correlationId, out var parsedCorrelationId))
            {
                context.CorrelationId = parsedCorrelationId;
            }
        }

        await next.Send(context);
    }

    public void Probe(ProbeContext context)
    {
        context.CreateFilterScope("correlationIdPublish");
    }
}
