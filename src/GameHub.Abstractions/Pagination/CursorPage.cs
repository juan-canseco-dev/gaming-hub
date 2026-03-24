namespace GameHub.Abstractions.Pagination;

public sealed class CursorPage<T>
{
    public IReadOnlyCollection<T> Items { get; init; } = Array.Empty<T>();
    public string? Next { get; init; }
}