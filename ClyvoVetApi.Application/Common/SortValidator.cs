using ClyvoVetApi.Application.Exceptions;

namespace ClyvoVetApi.Application.Common
{
    /// <summary>Valida o campo de ordenação informado contra a lista de campos permitidos do recurso.</summary>
    public static class SortValidator
    {
        public static void Ensure(PageQuery page, params string[] allowed)
        {
            if (string.IsNullOrWhiteSpace(page.SortBy))
                return;

            if (!allowed.Contains(page.SortBy, StringComparer.OrdinalIgnoreCase))
                throw new BusinessRuleException(
                    $"Campo de ordenação inválido. Use um destes: {string.Join(", ", allowed)}.");
        }
    }
}