using Microsoft.EntityFrameworkCore;

namespace Instafuel.Api.Common.Pagination;

public static class PagedQueryableExtensions
{
    /// <summary>
    /// Pages an ordered query. Callers should include a unique tie-breaker in
    /// the ordering so rows with equal sort values remain stable between pages.
    /// </summary>
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IOrderedQueryable<T> source,
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

