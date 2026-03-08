namespace GameHub.Domain.Shared;

public sealed class CursorPage<T>
{
    public IReadOnlyCollection<T> Items { get; init; } = Array.Empty<T>();
    public string? After { get; init; }
    public string? Before { get; init; }
}