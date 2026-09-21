using GameHub.Abstractions.Primitives;

namespace GameHub.Web.UI.Infrastructure.Http;

// Keeps transport metadata available while existing components display Error.Description.
public sealed record ProblemDetailsError(string Code, string Description) : Error(Code, Description)
{
    public string Type { get; init; } = "about:blank";
    public string? Title { get; init; }
    public string? Detail { get; init; }
    public int Status { get; init; }
    public string? Instance { get; init; }
    public string? CorrelationId { get; init; }
    public IReadOnlyDictionary<string, string[]> Errors { get; init; } = new Dictionary<string, string[]>();
}
