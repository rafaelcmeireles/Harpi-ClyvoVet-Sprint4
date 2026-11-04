using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ClyvoVetApi.Application.Abstractions;
using ClyvoVetApi.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ClyvoVetApi.Infrastructure.Security
{
    /// <summary>Configuração do JWT (seção <c>Jwt</c> do appsettings).</summary>
    public class JwtOptions
    {
        public const string SectionName = "Jwt";

        public string Issuer { get; set; } = "ClyvoVetApi";
        public string Audience { get; set; } = "ClyvoVetClients";

        /// <summary>Chave simétrica (mínimo 32 caracteres). Em produção, defina via variável de ambiente.</summary>
        public string Key { get; set; } = string.Empty;

        public int ExpirationMinutes { get; set; } = 60;
    }

    /// <summary>Emite tokens JWT (HS256) com as claims do usuário.</summary>
    public class JwtTokenService : ITokenService
    {
        private readonly JwtOptions _options;

        public JwtTokenService(IOptions<JwtOptions> options)
        {
            _options = options.Value;
        }

        public TokenResult Generate(User user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddMinutes(_options.ExpirationMinutes);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("name", user.Name),
                new Claim("role", user.Role)
            };

            var token = new JwtSecurityToken(
                issuer: _options.Issuer,
                audience: _options.Audience,
                claims: claims,
                expires: expires,
                signingCredentials: credentials);

            return new TokenResult(new JwtSecurityTokenHandler().WriteToken(token), expires);
        }
    }
}