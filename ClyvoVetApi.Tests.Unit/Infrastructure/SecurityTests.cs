using ClyvoVetApi.Infrastructure.Security;
using ClyvoVetApi.Models;
using FluentAssertions;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;

namespace ClyvoVetApi.Tests.Unit.Infrastructure;

public class SecurityTests
{
    private readonly Pbkdf2PasswordHasher _hasher = new();

    [Fact]
    public void Hash_MesmaSenha_GeraHashesDiferentesPorCausaDoSalt()
    {
        // Act
        var h1 = _hasher.Hash("senha123");
        var h2 = _hasher.Hash("senha123");

        // Assert
        h1.Should().NotBe(h2);
        h1.Should().NotContain("senha123");
    }

    [Fact]
    public void Verify_SenhaCorreta_RetornaTrue()
    {
        // Arrange
        var hash = _hasher.Hash("senha123");

        // Act & Assert
        _hasher.Verify(hash, "senha123").Should().BeTrue();
    }

    [Fact]
    public void Verify_SenhaIncorreta_RetornaFalse()
    {
        // Arrange
        var hash = _hasher.Hash("senha123");

        // Act & Assert
        _hasher.Verify(hash, "outra").Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("texto-qualquer")]
    [InlineData("v1.abc.def.ghi")]
    [InlineData("v1.1000.nao-base64.nao-base64")]
    public void Verify_HashMalformado_RetornaFalseSemLancar(string malformed)
    {
        // Act & Assert
        _hasher.Verify(malformed, "senha123").Should().BeFalse();
    }

    [Fact]
    public void Generate_UsuarioValido_EmiteJwtComClaimsEExpiracao()
    {
        // Arrange
        var options = Options.Create(new JwtOptions
        {
            Issuer = "emissor", Audience = "publico",
            Key = "chave-de-teste-com-mais-de-32-caracteres-123", ExpirationMinutes = 15
        });
        var service = new JwtTokenService(options);
        var user = new User { Id = 42, Name = "Dra. Ana", Email = "ana@test.com", Role = "Veterinario" };

        // Act
        var result = service.Generate(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result.AccessToken);

        // Assert
        jwt.Issuer.Should().Be("emissor");
        jwt.Audiences.Should().Contain("publico");
        jwt.Subject.Should().Be("42");
        jwt.Claims.Should().Contain(c => c.Type == "email" && c.Value == "ana@test.com");
        jwt.Claims.Should().Contain(c => c.Type == "role" && c.Value == "Veterinario");
        result.ExpiresAtUtc.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(15), TimeSpan.FromSeconds(10));
    }
}