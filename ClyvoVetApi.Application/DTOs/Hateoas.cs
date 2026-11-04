namespace ClyvoVetApi.DTOs
{
    /// <summary>Link HATEOAS: relação, URL e método HTTP da ação disponível.</summary>
    public record Link(string Rel, string Href, string Method);

    /// <summary>Recurso acompanhado dos links das ações disponíveis para ele.</summary>
    public class Resource<T>
    {
        public T Data { get; set; } = default!;
        public List<Link> Links { get; set; } = new();
    }

    /// <summary>Coleção paginada com metadados de paginação e links de navegação.</summary>
    public class PagedResponse<T>
    {
        public List<Resource<T>> Items { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public List<Link> Links { get; set; } = new();
    }

    // ======================== AUTH DTOs ========================

    /// <summary>Credenciais para obtenção do token JWT.</summary>
    public class LoginDto
    {
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "O e-mail é obrigatório.")]
        public string Email { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "A senha é obrigatória.")]
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>Token JWT emitido após autenticação.</summary>
    public class TokenResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string TokenType { get; set; } = "Bearer";
        public DateTime ExpiresAtUtc { get; set; }
    }

    // ======================== CHAT DTOs (MongoDB) ========================

    /// <summary>Mensagem enviada no chat interno de uma consulta.</summary>
    public class SendMessageDto
    {
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "O conteúdo da mensagem é obrigatório.")]
        [System.ComponentModel.DataAnnotations.StringLength(1000)]
        public string Content { get; set; } = string.Empty;
    }
}
