namespace GameHub.Application.Abstractions.Observability;

public interface ICorrelationIdAccessor
{
    string? CorrelationId { get; set; }
}
