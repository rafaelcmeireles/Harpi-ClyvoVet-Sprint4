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
public class PetServiceTests
{
    private readonly Mock<IPetRepository> _petsMock;
    private readonly Mock<IUserRepository> _usersMock;
    private readonly PetService _sut;
    private readonly DateTime _fixedNow;

    public PetServiceTests(FixedClockFixture clockFixture)
    {
        _petsMock = new Mock<IPetRepository>(MockBehavior.Strict);
        _usersMock = new Mock<IUserRepository>(MockBehavior.Strict);
        _fixedNow = clockFixture.FixedUtcNow;
        _sut = new PetService(_petsMock.Object, _usersMock.Object, clockFixture.Clock);
    }

    private static User BuildTutor(int id = 1) =>
        new() { Id = id, Name = "Maria", Email = "maria@test.com", Role = "Tutor" };

    private static User BuildVet(int id = 2) =>
        new() { Id = id, Name = "Dr. Carlos", Email = "carlos@test.com", Role = "Veterinario", Crmv = "CRMV-1" };

    private static Pet BuildPet(int id = 1, int tutorId = 1) =>
        new() { Id = id, Name = "Rex", Species = "Cachorro", TutorId = tutorId };

    // ==================== CreateAsync ====================

    [Fact]
    public async Task CreateAsync_TutorInexistente_LancaBusinessRuleException()
    {
        // Arrange
        _usersMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);
        var dto = new CreatePetDto { Name = "Rex", Species = "Cachorro", TutorId = 1 };

        // Act
        Func<Task> act = async () => await _sut.CreateAsync(dto);

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>();
    }

    [Fact]
    public async Task CreateAsync_UsuarioNaoETutor_LancaBusinessRuleException()
    {
        // Arrange
        var vet = BuildVet(id: 1);
        _usersMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(vet);
        var dto = new CreatePetDto { Name = "Rex", Species = "Cachorro", TutorId = 1 };

        // Act
        Func<Task> act = async () => await _sut.CreateAsync(dto);

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>();
    }

    [Fact]
    public async Task CreateAsync_DataDeNascimentoFutura_LancaBusinessRuleException()
    {
        // Arrange
        var tutor = BuildTutor();
        _usersMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(tutor);
        var dto = new CreatePetDto { Name = "Rex", Species = "Cachorro", TutorId = 1, BirthDate = _fixedNow.AddDays(1) };

        // Act
        Func<Task> act = async () => await _sut.CreateAsync(dto);

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>();
    }

    [Fact]
    public async Task CreateAsync_PesoInvalido_LancaBusinessRuleException()
    {
        // Arrange
        var tutor = BuildTutor();
        _usersMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(tutor);
        var dto = new CreatePetDto { Name = "Rex", Species = "Cachorro", TutorId = 1, WeightKg = -1 };

        // Act
        Func<Task> act = async () => await _sut.CreateAsync(dto);

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>();
    }

    [Fact]
    public async Task CreateAsync_DadosValidos_CriaPetComDataDoRelogio()
    {
        // Arrange
        var tutor = BuildTutor();
        _usersMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(tutor);
        _petsMock.Setup(r => r.AddAsync(It.IsAny<Pet>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _petsMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var dto = new CreatePetDto { Name = "Rex", Species = "Cachorro", TutorId = 1, WeightKg = 20 };

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        result.Name.Should().Be("Rex");
        result.CreatedAt.Should().Be(_fixedNow);
        _petsMock.Verify(r => r.AddAsync(It.Is<Pet>(p => p.TutorId == 1), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ==================== UpdateAsync ====================

    [Fact]
    public async Task UpdateAsync_PetInexistente_LancaNotFoundException()
    {
        // Arrange
        _petsMock.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((Pet?)null);
        var dto = new UpdatePetDto { Name = "Rex", Species = "Cachorro" };

        // Act
        Func<Task> act = async () => await _sut.UpdateAsync(999, dto);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_PesoInvalido_LancaBusinessRuleException()
    {
        // Arrange
        var pet = BuildPet();
        _petsMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(pet);
        var dto = new UpdatePetDto { Name = "Rex", Species = "Cachorro", WeightKg = 0 };

        // Act
        Func<Task> act = async () => await _sut.UpdateAsync(1, dto);

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>();
    }

    [Fact]
    public async Task UpdateAsync_DadosValidos_AtualizaESalva()
    {
        // Arrange
        var pet = BuildPet();
        _petsMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(pet);
        _petsMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var dto = new UpdatePetDto { Name = "Rex Atualizado", Species = "Cachorro", WeightKg = 22 };

        // Act
        var result = await _sut.UpdateAsync(1, dto);

        // Assert
        result.Name.Should().Be("Rex Atualizado");
        result.WeightKg.Should().Be(22);
    }

    // ==================== DeleteAsync ====================

    [Fact]
    public async Task DeleteAsync_PetExiste_RemoveESalva()
    {
        // Arrange
        var pet = BuildPet();
        _petsMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(pet);
        _petsMock.Setup(r => r.DeleteAsync(pet, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _petsMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        await _sut.DeleteAsync(1);

        // Assert
        _petsMock.Verify(r => r.DeleteAsync(pet, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_PetInexistente_LancaNotFoundException()
    {
        // Arrange
        _petsMock.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((Pet?)null);

        // Act
        Func<Task> act = async () => await _sut.DeleteAsync(999);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
