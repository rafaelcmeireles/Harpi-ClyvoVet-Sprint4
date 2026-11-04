using ClyvoVetApi.Application.Abstractions;
using ClyvoVetApi.Application.Exceptions;
using ClyvoVetApi.Application.Services;
using ClyvoVetApi.DTOs;
using ClyvoVetApi.Models;
using ClyvoVetApi.Tests.Unit.Application.Fixtures;
using FluentAssertions;
using Moq;

namespace ClyvoVetApi.Tests.Unit.Application;

[Collection("ApplicationServices")]
public class UserServiceTests
{
    private readonly Mock<IUserRepository> _usersMock;
    private readonly UserService _sut;
    private readonly DateTime _fixedNow;

    public UserServiceTests(FixedClockFixture clockFixture)
    {
        _usersMock = new Mock<IUserRepository>(MockBehavior.Strict);
        _fixedNow = clockFixture.FixedUtcNow;
        _sut = new UserService(_usersMock.Object, clockFixture.Clock, new Mock<IPasswordHasher>().Object);
    }

    private static User BuildUser(int id = 1, string role = "Tutor", string email = "maria@test.com", string? crmv = null) =>
        new() { Id = id, Name = "Maria", Email = email, Role = role, Crmv = crmv };

    // ==================== GetByIdAsync ====================

    [Fact]
    public async Task GetByIdAsync_UsuarioExiste_RetornaUsuario()
    {
        // Arrange
        var user = BuildUser();
        _usersMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        // Act
        var result = await _sut.GetByIdAsync(1);

        // Assert
        result.Should().Be(user);
    }

    [Fact]
    public async Task GetByIdAsync_UsuarioNaoExiste_LancaNotFoundException()
    {
        // Arrange
        _usersMock.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        // Act
        Func<Task> act = async () => await _sut.GetByIdAsync(999);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ==================== CreateAsync ====================

    [Fact]
    public async Task CreateAsync_RoleInvalido_LancaBusinessRuleException()
    {
        // Arrange
        var dto = new CreateUserDto { Name = "Teste", Email = "teste@test.com", Role = "Admin" };

        // Act
        Func<Task> act = async () => await _sut.CreateAsync(dto);

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>();
    }

    [Fact]
    public async Task CreateAsync_EmailDuplicado_LancaBusinessRuleException()
    {
        // Arrange
        var dto = new CreateUserDto { Name = "Maria", Email = "maria@test.com", Role = "Tutor" };
        _usersMock
            .Setup(r => r.EmailExistsAsync(dto.Email, It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        Func<Task> act = async () => await _sut.CreateAsync(dto);

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>();
    }

    [Fact]
    public async Task CreateAsync_VeterinarioSemCrmv_LancaBusinessRuleException()
    {
        // Arrange
        var dto = new CreateUserDto { Name = "Dr. Carlos", Email = "carlos@test.com", Role = "Veterinario" };
        _usersMock
            .Setup(r => r.EmailExistsAsync(dto.Email, It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        Func<Task> act = async () => await _sut.CreateAsync(dto);

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>();
    }

    [Fact]
    public async Task CreateAsync_DadosValidos_CriaUsuarioComDataDoRelogio()
    {
        // Arrange
        var dto = new CreateUserDto { Name = "Maria", Email = "maria@test.com", Role = "Tutor" };
        _usersMock
            .Setup(r => r.EmailExistsAsync(dto.Email, It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _usersMock
            .Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _usersMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        result.Email.Should().Be(dto.Email);
        result.CreatedAt.Should().Be(_fixedNow);
        _usersMock.Verify(r => r.AddAsync(It.Is<User>(u => u.Email == dto.Email), It.IsAny<CancellationToken>()), Times.Once);
        _usersMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ==================== UpdateAsync ====================

    [Fact]
    public async Task UpdateAsync_UsuarioNaoExiste_LancaNotFoundException()
    {
        // Arrange
        _usersMock.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);
        var dto = new UpdateUserDto { Name = "Teste", Email = "teste@test.com" };

        // Act
        Func<Task> act = async () => await _sut.UpdateAsync(999, dto);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_EmailJaUsadoPorOutroUsuario_LancaBusinessRuleException()
    {
        // Arrange
        var user = BuildUser(id: 1, email: "original@test.com");
        _usersMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _usersMock
            .Setup(r => r.EmailExistsAsync("novo@test.com", 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var dto = new UpdateUserDto { Name = "Maria", Email = "novo@test.com" };

        // Act
        Func<Task> act = async () => await _sut.UpdateAsync(1, dto);

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>();
    }

    [Fact]
    public async Task UpdateAsync_DadosValidos_AtualizaESalva()
    {
        // Arrange
        var user = BuildUser(id: 1, email: "original@test.com");
        _usersMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _usersMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var dto = new UpdateUserDto { Name = "Maria Atualizada", Email = "original@test.com" };

        // Act
        var result = await _sut.UpdateAsync(1, dto);

        // Assert
        result.Name.Should().Be("Maria Atualizada");
        _usersMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ==================== DeleteAsync ====================

    [Fact]
    public async Task DeleteAsync_UsuarioExiste_RemoveESalva()
    {
        // Arrange
        var user = BuildUser(id: 1);
        _usersMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _usersMock.Setup(r => r.DeleteAsync(user, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _usersMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        await _sut.DeleteAsync(1);

        // Assert
        _usersMock.Verify(r => r.DeleteAsync(user, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_UsuarioNaoExiste_LancaNotFoundException()
    {
        // Arrange
        _usersMock.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        // Act
        Func<Task> act = async () => await _sut.DeleteAsync(999);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
