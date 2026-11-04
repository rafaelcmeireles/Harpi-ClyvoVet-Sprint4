using ClyvoVetApi.Application.Abstractions;
using ClyvoVetApi.Application.Common;
using ClyvoVetApi.Application.Exceptions;
using ClyvoVetApi.Application.Services;
using ClyvoVetApi.Models;
using ClyvoVetApi.Tests.Unit.Application.Fixtures;
using FluentAssertions;
using Moq;

namespace ClyvoVetApi.Tests.Unit.Application;

[Collection("ApplicationServices")]
public class ChatServiceTests
{
    private readonly Mock<IChatMessageRepository> _messagesMock = new(MockBehavior.Strict);
    private readonly Mock<IConsultationRepository> _consultationsMock = new(MockBehavior.Strict);
    private readonly Mock<IPetRepository> _petsMock = new(MockBehavior.Strict);
    private readonly Mock<IUserRepository> _usersMock = new(MockBehavior.Strict);
    private readonly ChatService _sut;
    private readonly DateTime _now;

    private const int ConsultationId = 10, VetId = 2, TutorId = 1, PetId = 5;

    public ChatServiceTests(FixedClockFixture clockFixture)
    {
        _now = clockFixture.FixedUtcNow;
        _sut = new ChatService(_messagesMock.Object, _consultationsMock.Object, _petsMock.Object,
            _usersMock.Object, clockFixture.Clock);
    }

    private void SetupConsultation() =>
        _consultationsMock.Setup(r => r.GetByIdAsync(ConsultationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Consultation { Id = ConsultationId, PetId = PetId, VetId = VetId });

    private void SetupPet() =>
        _petsMock.Setup(r => r.GetByIdAsync(PetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Pet { Id = PetId, TutorId = TutorId });

    [Fact]
    public async Task SendAsync_MensagemVazia_LancaBusinessRuleException()
    {
        // Act
        var act = () => _sut.SendAsync(ConsultationId, VetId, "   ");

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>();
    }

    [Fact]
    public async Task SendAsync_ConsultaInexistente_LancaNotFoundException()
    {
        // Arrange
        _consultationsMock.Setup(r => r.GetByIdAsync(ConsultationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Consultation?)null);

        // Act
        var act = () => _sut.SendAsync(ConsultationId, VetId, "Olá");

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task SendAsync_RemetenteInexistente_LancaNotFoundException()
    {
        // Arrange
        SetupConsultation();
        _usersMock.Setup(r => r.GetByIdAsync(VetId, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        // Act
        var act = () => _sut.SendAsync(ConsultationId, VetId, "Olá");

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task SendAsync_VeterinarioDaConsulta_SalvaMensagem()
    {
        // Arrange
        SetupConsultation();
        _usersMock.Setup(r => r.GetByIdAsync(VetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = VetId, Role = "Veterinario" });
        _messagesMock.Setup(r => r.AddAsync(It.IsAny<ChatMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var message = await _sut.SendAsync(ConsultationId, VetId, "  Bom dia!  ");

        // Assert
        message.Content.Should().Be("Bom dia!");
        message.SenderRole.Should().Be("Veterinario");
        message.SentAt.Should().Be(_now);
        _messagesMock.Verify(r => r.AddAsync(message, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendAsync_TutorDoPet_SalvaMensagem()
    {
        // Arrange
        SetupConsultation();
        SetupPet();
        _usersMock.Setup(r => r.GetByIdAsync(TutorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = TutorId, Role = "Tutor" });
        _messagesMock.Setup(r => r.AddAsync(It.IsAny<ChatMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var message = await _sut.SendAsync(ConsultationId, TutorId, "Obrigado");

        // Assert
        message.SenderId.Should().Be(TutorId);
    }

    [Fact]
    public async Task SendAsync_UsuarioNaoParticipante_LancaForbidden()
    {
        // Arrange
        SetupConsultation();
        SetupPet();
        _usersMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = 99, Role = "Tutor" });

        // Act
        var act = () => _sut.SendAsync(ConsultationId, 99, "Intruso");

        // Assert
        await act.Should().ThrowAsync<ForbiddenAppException>();
        _messagesMock.Verify(r => r.AddAsync(It.IsAny<ChatMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetMessagesAsync_ConsultaExiste_RetornaMensagensPaginadas()
    {
        // Arrange
        SetupConsultation();
        var page = new PageQuery();
        var expected = new PagedResult<ChatMessage>(new List<ChatMessage> { new() { Content = "a" } }, 1);
        _messagesMock.Setup(r => r.GetByConsultationAsync(ConsultationId, page, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _sut.GetMessagesAsync(ConsultationId, page);

        // Assert
        result.Should().Be(expected);
    }
}