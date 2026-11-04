using ClyvoVetApi.Application.Abstractions;
using ClyvoVetApi.Application.Exceptions;
using ClyvoVetApi.DTOs;

namespace ClyvoVetApi.Application.Services
{
    /// <summary>Caso de uso de autenticação: valida credenciais e emite o token JWT.</summary>
    public interface IAuthService
    {
        Task<TokenResponse> LoginAsync(LoginDto dto, CancellationToken ct = default);
    }

    public class AuthService : IAuthService
    {
        private readonly IUserRepository _users;
        private readonly IPasswordHasher _hasher;
        private readonly ITokenService _tokens;

        public AuthService(IUserRepository users, IPasswordHasher hasher, ITokenService tokens)
        {
            _users = users;
            _hasher = hasher;
            _tokens = tokens;
        }

        public async Task<TokenResponse> LoginAsync(LoginDto dto, CancellationToken ct = default)
        {
            var user = await _users.GetByEmailAsync(dto.Email, ct);

            // Mesma mensagem para usuário inexistente, sem senha cadastrada ou senha errada:
            // evita revelar quais e-mails existem na base.
            if (user is null || string.IsNullOrEmpty(user.PasswordHash) ||
                !_hasher.Verify(user.PasswordHash, dto.Password))
            {
                throw new UnauthorizedAppException("E-mail ou senha inválidos.");
            }

            var token = _tokens.Generate(user);
            return new TokenResponse { AccessToken = token.AccessToken, ExpiresAtUtc = token.ExpiresAtUtc };
        }
    }
}