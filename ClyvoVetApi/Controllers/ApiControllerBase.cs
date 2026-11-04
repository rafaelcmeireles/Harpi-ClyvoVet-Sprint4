using ClyvoVetApi.Application.Common;
using ClyvoVetApi.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace ClyvoVetApi.Controllers
{
    /// <summary>
    /// Base dos controllers: concentra a montagem de respostas HATEOAS e paginadas
    /// para que os controllers concretos permaneçam finos.
    /// </summary>
    [ApiController]
    [Produces("application/json")]
    public abstract class ApiControllerBase : ControllerBase
    {
        /// <summary>Converte um caminho relativo em URL absoluta da requisição atual.</summary>
        protected string Abs(string path) => $"{Request.Scheme}://{Request.Host}{Request.PathBase}{path}";

        protected Resource<T> Wrap<T>(T data, IEnumerable<Link> links) =>
            new() { Data = data, Links = links.ToList() };

        /// <summary>
        /// Monta a resposta paginada: cada item recebe seus links e a coleção recebe
        /// os links de navegação (self, first, prev, next, last).
        /// </summary>
        protected PagedResponse<T> ToPaged<T>(
            PagedResult<T> result,
            PageQuery page,
            string basePath,
            IDictionary<string, string?> filters,
            Func<T, IEnumerable<Link>> linksFor)
        {
            var totalPages = (int)Math.Ceiling(result.TotalItems / (double)page.PageSize);

            string PageUrl(int number)
            {
                var query = filters
                    .Where(f => !string.IsNullOrWhiteSpace(f.Value))
                    .ToDictionary(f => f.Key, f => f.Value!);

                query["page"] = number.ToString();
                query["pageSize"] = page.PageSize.ToString();
                if (!string.IsNullOrWhiteSpace(page.SortBy)) query["sortBy"] = page.SortBy!;
                if (page.Desc) query["desc"] = "true";

                var qs = string.Join("&", query.Select(q => $"{q.Key}={Uri.EscapeDataString(q.Value)}"));
                return Abs($"{basePath}?{qs}");
            }

            var links = new List<Link>
            {
                new("self", PageUrl(page.Page), "GET"),
                new("first", PageUrl(1), "GET")
            };

            if (page.Page > 1)
                links.Add(new Link("prev", PageUrl(page.Page - 1), "GET"));
            if (page.Page < totalPages)
                links.Add(new Link("next", PageUrl(page.Page + 1), "GET"));

            links.Add(new Link("last", PageUrl(Math.Max(totalPages, 1)), "GET"));

            return new PagedResponse<T>
            {
                Items = result.Items.Select(i => Wrap(i, linksFor(i))).ToList(),
                Page = page.Page,
                PageSize = page.PageSize,
                TotalItems = result.TotalItems,
                TotalPages = totalPages,
                Links = links
            };
        }
    }
}