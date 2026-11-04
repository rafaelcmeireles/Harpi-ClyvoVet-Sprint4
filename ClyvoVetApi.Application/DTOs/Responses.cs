namespace ClyvoVetApi.DTOs
{
    /// <summary>
    /// Formato padronizado de erro retornado pelo middleware global de exceções.
    /// O CorrelationId permite localizar, nos logs estruturados, exatamente a requisição
    /// que originou o erro.
    /// </summary>
    public class ApiErrorResponse
    {
        public string Message { get; set; } = string.Empty;
        public string? CorrelationId { get; set; }
    }
}
