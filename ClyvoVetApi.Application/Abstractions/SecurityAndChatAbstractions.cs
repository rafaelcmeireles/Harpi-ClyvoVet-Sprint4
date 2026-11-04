using ClyvoVetApi.Application.Common;
using ClyvoVetApi.Models;

namespace ClyvoVetApi.Application.Abstractions
{
    /// <summary>Hash e verificação de senhas (implementação na camada de Infraestrutura).</summary>
    public interface IPasswordHasher
    {
        string Hash(string password);
        bool Verify(string passwordHash, string password);
    }

    public record TokenResult(string AccessToken, DateTime ExpiresAtUtc);

    /// <summary>Emissão de tokens de acesso (JWT).</summary>
    public interface ITokenService
    {
        TokenResult Generate(User user);
    }

    /// <summary>Repositório NoSQL (MongoDB) das mensagens do chat.</summary>
    public interface IChatMessageRepository
    {
        Task AddAsync(ChatMessage message, CancellationToken ct = default);

        Task<PagedResult<ChatMessage>> GetByConsultationAsync(
            int consultationId, PageQuery page, CancellationToken ct = default);
    }
}
