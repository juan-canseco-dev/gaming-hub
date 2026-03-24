namespace GameHub.Abstractions.Pagination;

public class PaginatedList<TEntity>
{
    public IReadOnlyCollection<TEntity> Items { get; } = default!;
    public int PageNumber { get; }
    public int TotalPages { get; }
    public int TotalCount { get; }

    private PaginatedList() { }

    public PaginatedList(IReadOnlyCollection<TEntity> items, int totalCount, int pageNumber, int totalPages)
    {
        Items = items;
        TotalCount = totalCount;
        PageNumber = pageNumber;
        TotalPages = totalPages;
    }

    public bool HasPreviousPage => PageNumber > 1;

    public bool HasNextPage => PageNumber < TotalPages;
}