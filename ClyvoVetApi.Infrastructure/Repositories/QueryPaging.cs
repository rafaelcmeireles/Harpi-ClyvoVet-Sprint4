using System.Linq.Expressions;
using ClyvoVetApi.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace ClyvoVetApi.Infrastructure.Repositories
{
    /// <summary>Helpers de paginação/ordenação reutilizados pelos repositórios EF.</summary>
    internal static class QueryPaging
    {
        public static IOrderedQueryable<T> Order<T, TKey>(
            this IQueryable<T> query, Expression<Func<T, TKey>> key, bool desc) =>
            desc ? query.OrderByDescending(key) : query.OrderBy(key);

        public static async Task<PagedResult<T>> ToPagedAsync<T>(
            this IQueryable<T> orderedQuery, PageQuery page, CancellationToken ct)
        {
            var total = await orderedQuery.CountAsync(ct);
            var items = await orderedQuery
                .Skip((page.Page - 1) * page.PageSize)
                .Take(page.PageSize)
                .ToListAsync(ct);

            return new PagedResult<T>(items, total);
        }
    }
}