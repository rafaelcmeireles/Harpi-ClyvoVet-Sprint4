using ClyvoVetApi.Application.Abstractions;
using ClyvoVetApi.Application.Exceptions;
using ClyvoVetApi.Application.Services;
using ClyvoVetApi.DTOs;
using ClyvoVetApi.Models;
using FluentAssertions;
using Moq;

namespace ClyvoVetApi.Tests.Unit.Application;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _usersMock = new(MockBehavior.Strict);
    private readonly Mock<IPasswordHasher> _hasherMock = new(MockBehavior.Strict);
    private readonly Mock<ITokenService> _tokensMock = new(MockBehavior.Strict);
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _sut = new AuthService(_usersMock.Object, _hasherMock.Object, _tokensMock.Object);
    }

    [Fact]
    public async Task LoginAsync_CredenciaisValidas_RetornaToken()
    {
        // Arrange
        var user = new User { Id = 7, Email = "a@test.com", Role = "Tutor", PasswordHash = "H" };
        var expires = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        _usersMock.Setup(r => r.GetByEmailAsync("a@test.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _hasherMock.Setup(h => h.Verify("H", "senha123")).Returns(true);
        _tokensMock.Setup(t => t.Generate(user)).Returns(new TokenResult("jwt-token", expires));

        // Act
        var result = await _sut.LoginAsync(new LoginDto { Email = "a@test.com", Password = "senha123" });

        // Assert
        result.AccessToken.Should().Be("jwt-token");
        result.ExpiresAtUtc.Should().Be(expires);
        result.TokenType.Should().Be("Bearer");
    }

    [Fact]
    public async Task LoginAsync_UsuarioInexistente_LancaUnauthorized()
    {
        // Arrange
        _usersMock.Setup(r => r.GetByEmailAsync("x@test.com", It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        // Act
        var act = () => _sut.LoginAsync(new LoginDto { Email = "x@test.com", Password = "qualquer" });

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAppException>();
    }

    [Fact]
    public async Task LoginAsync_UsuarioSemSenhaCadastrada_LancaUnauthorized()
    {
        // Arrange
        var user = new User { Id = 1, Email = "a@test.com", PasswordHash = null };
        _usersMock.Setup(r => r.GetByEmailAsync("a@test.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);

        // Act
        var act = () => _sut.LoginAsync(new LoginDto { Email = "a@test.com", Password = "x" });

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAppException>();
    }

    [Fact]
    public async Task LoginAsync_SenhaIncorreta_LancaUnauthorized()
    {
        // Arrange
        var user = new User { Id = 1, Email = "a@test.com", PasswordHash = "H" };
        _usersMock.Setup(r => r.GetByEmailAsync("a@test.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _hasherMock.Setup(h => h.Verify("H", "errada")).Returns(false);

        // Act
        var act = () => _sut.LoginAsync(new LoginDto { Email = "a@test.com", Password = "errada" });

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAppException>();
        _tokensMock.Verify(t => t.Generate(It.IsAny<User>()), Times.Never);
    }
}