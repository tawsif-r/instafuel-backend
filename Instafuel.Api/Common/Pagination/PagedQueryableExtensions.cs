using Microsoft.EntityFrameworkCore;

namespace Instafuel.Api.Common.Pagination;

public static class PagedQueryableExtensions
{
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> source,
        PagedRequest request,
        CancellationToken cancellationToken = default)
    {
        var total = await source.CountAsync(cancellationToken);
        if (total == 0)
        {
            return PagedResult<T>.Empty(request.Page, request.PageSize);
        }

        var items = await source
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<T>(items, total, request.Page, request.PageSize);
    }
}

