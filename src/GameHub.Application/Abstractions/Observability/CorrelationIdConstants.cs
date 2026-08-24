namespace GameHub.Application.Abstractions.Observability;

public static class CorrelationIdConstants
{
    public const string HeaderName = "X-Correlation-ID";
    public const string LogPropertyName = "CorrelationId";
    public const int MaxLength = 64;
}
