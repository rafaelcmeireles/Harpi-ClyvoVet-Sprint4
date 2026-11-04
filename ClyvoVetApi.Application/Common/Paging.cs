using System.ComponentModel.DataAnnotations;

namespace ClyvoVetApi.Application.Common
{
    /// <summary>
    /// Parâmetros de paginação e ordenação aceitos pelos endpoints de consulta
    /// (<c>?page=1&amp;pageSize=10&amp;sortBy=name&amp;desc=false</c>).
    /// </summary>
    public class PageQuery
    {
        public const int MaxPageSize = 100;

        /// <summary>Número da página (começa em 1).</summary>
        [Range(1, int.MaxValue, ErrorMessage = "page deve ser maior ou igual a 1.")]
        public int Page { get; set; } = 1;

        /// <summary>Quantidade de itens por página (1 a 100).</summary>
        [Range(1, MaxPageSize, ErrorMessage = "pageSize deve estar entre 1 e 100.")]
        public int PageSize { get; set; } = 10;

        /// <summary>Campo de ordenação (varia por recurso). Padrão: id.</summary>
        public string? SortBy { get; set; }

        /// <summary>Quando verdadeiro, ordena de forma decrescente.</summary>
        public bool Desc { get; set; }
    }

    /// <summary>Resultado paginado devolvido pelos repositórios.</summary>
    public record PagedResult<T>(IReadOnlyList<T> Items, int TotalItems);

    /// <summary>Filtros opcionais da listagem de usuários.</summary>
    public record UserFilter(string? Role = null, string? Search = null);

    /// <summary>Filtros opcionais da listagem de pets.</summary>
    public record PetFilter(int? TutorId = null, string? Species = null, string? Breed = null, string? Search = null);

    /// <summary>Filtros opcionais da listagem de consultas.</summary>
    public record ConsultationFilter(
        int? PetId = null,
        int? VetId = null,
        string? Status = null,
        DateTime? From = null,
        DateTime? To = null);
}
