using GameHub.Abstractions.Pagination;
using Microsoft.EntityFrameworkCore;

namespace GameHub.Application.Extensions;

public static class QueryableExtensions
{
    public static async Task<PaginatedList<TEntity>> ToPaginatedListAsync<TEntity>(
        this IQueryable<TEntity> source, 
        int pageNumber, 
        int pageSize, 
        CancellationToken cancellationToken = default) where TEntity : class
    {
        var count = await source.CountAsync(cancellationToken);
        
        var items = await source.Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return new PaginatedList<TEntity>(
            items: items,
            totalCount: count,
            pageNumber: pageNumber,
            totalPages: (int)Math.Ceiling(count / (double)pageSize)
        );
    }
}
