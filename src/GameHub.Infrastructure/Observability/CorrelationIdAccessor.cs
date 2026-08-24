using GameHub.Application.Abstractions.Observability;

namespace GameHub.Infrastructure.Observability;

public sealed class CorrelationIdAccessor : ICorrelationIdAccessor
{
    private static readonly AsyncLocal<CorrelationIdHolder?> Current = new();

    public string? CorrelationId
    {
        get => Current.Value?.Value;
        set
        {
            var holder = Current.Value;
            if (holder is not null)
            {
                holder.Value = null;
            }

            if (!string.IsNullOrWhiteSpace(value))
            {
                Current.Value = new CorrelationIdHolder { Value = value };
            }
        }
    }

    private sealed class CorrelationIdHolder
    {
        public string? Value { get; set; }
    }
}
