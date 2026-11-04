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
public class ConsultationServiceTests
{
    private readonly Mock<IConsultationRepository> _consultationsMock;
    private readonly Mock<IPetRepository> _petsMock;
    private readonly Mock<IUserRepository> _usersMock;
    private readonly ConsultationService _sut;
    private readonly DateTime _fixedNow;

    public ConsultationServiceTests(FixedClockFixture clockFixture)
    {
        _consultationsMock = new Mock<IConsultationRepository>(MockBehavior.Strict);
        _petsMock = new Mock<IPetRepository>(MockBehavior.Strict);
        _usersMock = new Mock<IUserRepository>(MockBehavior.Strict);
        _fixedNow = clockFixture.FixedUtcNow;
        _sut = new ConsultationService(_consultationsMock.Object, _petsMock.Object, _usersMock.Object, clockFixture.Clock);
    }

    private static Pet BuildPet(int id = 1) =>
        new() { Id = id, Name = "Rex", Species = "Cachorro", TutorId = 1 };

    private static User BuildVet(int id = 2) =>
        new() { Id = id, Name = "Dr. Carlos", Email = "carlos@test.com", Role = "Veterinario", Crmv = "CRMV-1" };

    private static User BuildTutor(int id = 3) =>
        new() { Id = id, Name = "Maria", Email = "maria@test.com", Role = "Tutor" };

    private static Consultation BuildConsultation(int id = 1, int vetId = 2, int petId = 1, string status = "Agendada", DateTime? date = null) =>
        new() { Id = id, VetId = vetId, PetId = petId, Status = status, ConsultationDate = date ?? new DateTime(2026, 9, 20) };

    // ==================== CreateAsync ====================

    [Fact]
    public async Task CreateAsync_PetInexistente_LancaBusinessRuleException()
    {
        // Arrange
        _petsMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((Pet?)null);
        var dto = new CreateConsultationDto { PetId = 1, VetId = 2, ConsultationDate = _fixedNow.AddDays(1) };

        // Act
        Func<Task> act = async () => await _sut.CreateAsync(dto);

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>();
    }

    [Fact]
    public async Task CreateAsync_VetInexistente_LancaBusinessRuleException()
    {
        // Arrange
        var pet = BuildPet();
        _petsMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(pet);
        _usersMock.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);
        var dto = new CreateConsultationDto { PetId = 1, VetId = 2, ConsultationDate = _fixedNow.AddDays(1) };

        // Act
        Func<Task> act = async () => await _sut.CreateAsync(dto);

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>();
    }

    [Fact]
    public async Task CreateAsync_UsuarioNaoEVeterinario_LancaBusinessRuleException()
    {
        // Arrange
        var pet = BuildPet();
        var tutor = BuildTutor(id: 2);
        _petsMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(pet);
        _usersMock.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(tutor);
        var dto = new CreateConsultationDto { PetId = 1, VetId = 2, ConsultationDate = _fixedNow.AddDays(1) };

        // Act
        Func<Task> act = async () => await _sut.CreateAsync(dto);

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>();
    }

    [Fact]
    public async Task CreateAsync_DataNoPassado_LancaBusinessRuleException()
    {
        // Arrange
        var pet = BuildPet();
        var vet = BuildVet();
        _petsMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(pet);
        _usersMock.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(vet);
        var dto = new CreateConsultationDto { PetId = 1, VetId = 2, ConsultationDate = _fixedNow.AddDays(-1) };

        // Act
        Func<Task> act = async () => await _sut.CreateAsync(dto);

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>();
    }

    [Fact]
    public async Task CreateAsync_ConflitoDeHorario_LancaBusinessRuleException()
    {
        // Arrange
        var pet = BuildPet();
        var vet = BuildVet();
        _petsMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(pet);
        _usersMock.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(vet);
        _consultationsMock
            .Setup(r => r.HasScheduleConflictAsync(2, It.IsAny<DateTime>(), ConsultationService.MinimumGapBetweenConsultations, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var dto = new CreateConsultationDto { PetId = 1, VetId = 2, ConsultationDate = _fixedNow.AddDays(1) };

        // Act
        Func<Task> act = async () => await _sut.CreateAsync(dto);

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>();
    }

    [Fact]
    public async Task CreateAsync_DadosValidos_CriaConsultaComStatusAgendada()
    {
        // Arrange
        var pet = BuildPet();
        var vet = BuildVet();
        _petsMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(pet);
        _usersMock.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(vet);
        _consultationsMock
            .Setup(r => r.HasScheduleConflictAsync(2, It.IsAny<DateTime>(), ConsultationService.MinimumGapBetweenConsultations, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _consultationsMock.Setup(r => r.AddAsync(It.IsAny<Consultation>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _consultationsMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var dto = new CreateConsultationDto { PetId = 1, VetId = 2, ConsultationDate = _fixedNow.AddDays(1) };

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        result.Status.Should().Be("Agendada");
        result.CreatedAt.Should().Be(_fixedNow);
    }

    // ==================== UpdateAsync ====================

    [Fact]
    public async Task UpdateAsync_ConsultaInexistente_LancaNotFoundException()
    {
        // Arrange
        _consultationsMock.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((Consultation?)null);
        var dto = new UpdateConsultationDto { ConsultationDate = _fixedNow.AddDays(1) };

        // Act
        Func<Task> act = async () => await _sut.UpdateAsync(999, dto);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_StatusInvalido_LancaBusinessRuleException()
    {
        // Arrange
        var consultation = BuildConsultation();
        _consultationsMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(consultation);
        var dto = new UpdateConsultationDto { ConsultationDate = _fixedNow.AddDays(1), Status = "Finalizada" };

        // Act
        Func<Task> act = async () => await _sut.UpdateAsync(1, dto);

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>();
    }

    [Fact]
    public async Task UpdateAsync_ConsultaJaRealizada_LancaBusinessRuleException()
    {
        // Arrange
        var consultation = BuildConsultation(status: "Realizada");
        _consultationsMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(consultation);
        var dto = new UpdateConsultationDto { ConsultationDate = _fixedNow.AddDays(1), Status = "Agendada" };

        // Act
        Func<Task> act = async () => await _sut.UpdateAsync(1, dto);

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>();
    }

    [Fact]
    public async Task UpdateAsync_DataNoPassado_LancaBusinessRuleException()
    {
        // Arrange
        var consultation = BuildConsultation();
        _consultationsMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(consultation);
        var dto = new UpdateConsultationDto { ConsultationDate = _fixedNow.AddDays(-1) };

        // Act
        Func<Task> act = async () => await _sut.UpdateAsync(1, dto);

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>();
    }

    [Fact]
    public async Task UpdateAsync_DadosValidos_AtualizaESalva()
    {
        // Arrange
        var consultation = BuildConsultation();
        _consultationsMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(consultation);
        _consultationsMock
            .Setup(r => r.HasScheduleConflictAsync(2, It.IsAny<DateTime>(), ConsultationService.MinimumGapBetweenConsultations, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _consultationsMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var dto = new UpdateConsultationDto { ConsultationDate = _fixedNow.AddDays(1), Status = "Realizada", Description = "Consulta de rotina" };

        // Act
        var result = await _sut.UpdateAsync(1, dto);

        // Assert
        result.Status.Should().Be("Realizada");
        result.Description.Should().Be("Consulta de rotina");
    }

    // ==================== GetByPeriodAsync ====================

    [Fact]
    public async Task GetByPeriodAsync_DataInicioMaiorQueFim_LancaBusinessRuleException()
    {
        // Arrange
        var start = _fixedNow;
        var end = _fixedNow.AddDays(-1);

        // Act
        Func<Task> act = async () => await _sut.GetByPeriodAsync(start, end);

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>();
    }

    // ==================== DeleteAsync ====================

    [Fact]
    public async Task DeleteAsync_ConsultaInexistente_LancaNotFoundException()
    {
        // Arrange
        _consultationsMock.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((Consultation?)null);

        // Act
        Func<Task> act = async () => await _sut.DeleteAsync(999);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeleteAsync_ConsultaExiste_RemoveESalva()
    {
        // Arrange
        var consultation = BuildConsultation();
        _consultationsMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(consultation);
        _consultationsMock.Setup(r => r.DeleteAsync(consultation, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _consultationsMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        await _sut.DeleteAsync(1);

        // Assert
        _consultationsMock.Verify(r => r.DeleteAsync(consultation, It.IsAny<CancellationToken>()), Times.Once);
    }
}
