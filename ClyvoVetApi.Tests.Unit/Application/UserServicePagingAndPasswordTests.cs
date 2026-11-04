using ClyvoVetApi.Application.Abstractions;
using ClyvoVetApi.Application.Common;
using ClyvoVetApi.Application.Exceptions;
using ClyvoVetApi.Application.Services;
using ClyvoVetApi.DTOs;
using ClyvoVetApi.Models;
using ClyvoVetApi.Tests.Unit.Application.Fixtures;
using FluentAssertions;
using Moq;

namespace ClyvoVetApi.Tests.Unit.Application;

[Collection("ApplicationServices")]
public class UserServicePagingAndPasswordTests
{
    private readonly Mock<IUserRepository> _usersMock = new(MockBehavior.Strict);
    private readonly Mock<IPasswordHasher> _hasherMock = new(MockBehavior.Strict);
    private readonly UserService _sut;

    public UserServicePagingAndPasswordTests(FixedClockFixture clockFixture)
    {
        _sut = new UserService(_usersMock.Object, clockFixture.Clock, _hasherMock.Object);
    }

    [Fact]
    public async Task GetPagedAsync_FiltrosValidos_DelegaParaRepositorio()
    {
        // Arrange
        var page = new PageQuery { Page = 2, PageSize = 5, SortBy = "name" };
        var filter = new UserFilter("Tutor", "ma");
        var expected = new PagedResult<User>(new List<User> { new() { Id = 1 } }, 11);
        _usersMock.Setup(r => r.GetPagedAsync(filter, page, It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        // Act
        var result = await _sut.GetPagedAsync(filter, page);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public async Task GetPagedAsync_CampoDeOrdenacaoInvalido_LancaBusinessRuleException()
    {
        // Arrange
        var page = new PageQuery { SortBy = "senha" };

        // Act
        var act = () => _sut.GetPagedAsync(new UserFilter(), page);

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>().WithMessage("*ordenação*");
    }

    [Fact]
    public async Task GetPagedAsync_RoleInvalido_LancaBusinessRuleException()
    {
        // Act
        var act = () => _sut.GetPagedAsync(new UserFilter(Role: "Admin"), new PageQuery());

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>();
    }

    [Fact]
    public async Task CreateAsync_ComSenha_ArmazenaApenasOHash()
    {
        // Arrange
        User? saved = null;
        _usersMock.Setup(r => r.EmailExistsAsync("ana@test.com", null, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _usersMock.Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((u, _) => saved = u).Returns(Task.CompletedTask);
        _usersMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _hasherMock.Setup(h => h.Hash("segredo123")).Returns("HASH");

        // Act
        await _sut.CreateAsync(new CreateUserDto
        {
            Name = "Ana", Email = "ana@test.com", Role = "Tutor", Password = "segredo123"
        });

        // Assert
        saved!.PasswordHash.Should().Be("HASH");
    }

    [Fact]
    public async Task CreateAsync_SemSenha_NaoGeraHash()
    {
        // Arrange
        User? saved = null;
        _usersMock.Setup(r => r.EmailExistsAsync("bia@test.com", null, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _usersMock.Setup(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((u, _) => saved = u).Returns(Task.CompletedTask);
        _usersMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        await _sut.CreateAsync(new CreateUserDto { Name = "Bia", Email = "bia@test.com", Role = "Tutor" });

        // Assert
        saved!.PasswordHash.Should().BeNull();
        _hasherMock.Verify(h => h.Hash(It.IsAny<string>()), Times.Never);
    }
}