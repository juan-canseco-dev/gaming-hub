using AutoMapper;
using AutoMapper.QueryableExtensions;
using GameHub.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace GameHub.Application.Extensions;

public static class MappingExtensions
{
    public static Task<PaginatedList<TDestination>> PaginatedListAsync<TDestination>(this IQueryable queryable, IConfigurationProvider configuration, int pageNumber, int pageSize, CancellationToken cancellationToken = default) where TDestination : class
        => queryable.ProjectTo<TDestination>(configuration).ToPaginatedListAsync(pageNumber, pageSize, cancellationToken);


    public static Task<List<TDestination>> ProjectToListAsync<TDestination>(this IQueryable queryable, IConfigurationProvider configuration, CancellationToken cancellationToken = default) where TDestination : class
        => queryable.ProjectTo<TDestination>(configuration).AsNoTracking().ToListAsync(cancellationToken);
}