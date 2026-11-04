using System.Net;
using System.Net.Http.Json;
using ClyvoVetApi.DTOs;
using ClyvoVetApi.Models;
using ClyvoVetApi.Tests.Integration.Fixtures;
using FluentAssertions;

namespace ClyvoVetApi.Tests.Integration.Controllers;

[Collection("Integration")]
public class ChatIntegrationTests
{
    private readonly CustomWebApplicationFactory _factory;

    public ChatIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<(User Tutor, User Vet, Consultation Consultation)> SeedAsync()
    {
        var admin = _factory.CreateAuthenticatedClient();

        var tutor = (await (await admin.PostAsJsonAsync("/api/users", new CreateUserDto
        {
            Name = "Tutor Chat", Email = $"tc_{Guid.NewGuid()}@test.com", Role = "Tutor"
        })).Content.ReadFromJsonAsync<User>())!;

        var vet = (await (await admin.PostAsJsonAsync("/api/users", new CreateUserDto
        {
            Name = "Vet Chat", Email = $"vc_{Guid.NewGuid()}@test.com", Role = "Veterinario", Crmv = "SP-9876"
        })).Content.ReadFromJsonAsync<User>())!;

        var pet = (await (await admin.PostAsJsonAsync("/api/pets", new CreatePetDto
        {
            Name = "Bidu", Species = "Cachorro", TutorId = tutor.Id
        })).Content.ReadFromJsonAsync<Pet>())!;

        var consultationResponse = await admin.PostAsJsonAsync("/api/consultations", new CreateConsultationDto
        {
            PetId = pet.Id, VetId = vet.Id, ConsultationDate = DateTime.UtcNow.AddDays(3)
        });
        consultationResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var consultation = (await consultationResponse.Content.ReadFromJsonAsync<Consultation>())!;

        return (tutor, vet, consultation);
    }

    [Fact]
    public async Task Send_TutorEVeterinarioConversam_MensagensListadasEmOrdemCronologica()
    {
        // Arrange
        var (tutor, vet, consultation) = await SeedAsync();
        var tutorClient = _factory.CreateAuthenticatedClient("Tutor", tutor.Id);
        var vetClient = _factory.CreateAuthenticatedClient("Veterinario", vet.Id);
        var url = $"/api/consultations/{consultation.Id}/messages";

        // Act
        var first = await tutorClient.PostAsJsonAsync(url, new SendMessageDto { Content = "Bom dia, doutor!" });
        await Task.Delay(10);
        var second = await vetClient.PostAsJsonAsync(url, new SendMessageDto { Content = "Bom dia! Como o Bidu está?" });
        var page = await vetClient.GetFromJsonAsync<PagedResponse<ChatMessage>>(url);

        // Assert
        first.StatusCode.Should().Be(HttpStatusCode.Created);
        second.StatusCode.Should().Be(HttpStatusCode.Created);
        page!.TotalItems.Should().Be(2);
        page.Items.Select(i => i.Data.Content).Should().Equal("Bom dia, doutor!", "Bom dia! Como o Bidu está?");
        page.Items.Select(i => i.Data.SenderId).Should().Equal(tutor.Id, vet.Id);
    }

    [Fact]
    public async Task Send_UsuarioQueNaoParticipaDaConsulta_RetornaForbidden()
    {
        // Arrange
        var (_, _, consultation) = await SeedAsync();
        var intruder = (await (await _factory.CreateAuthenticatedClient().PostAsJsonAsync("/api/users", new CreateUserDto
        {
            Name = "Intruso", Email = $"in_{Guid.NewGuid()}@test.com", Role = "Tutor"
        })).Content.ReadFromJsonAsync<User>())!;
        var client = _factory.CreateAuthenticatedClient("Tutor", intruder.Id);

        // Act
        var response = await client.PostAsJsonAsync(
            $"/api/consultations/{consultation.Id}/messages", new SendMessageDto { Content = "Oi" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Send_ConsultaInexistente_RetornaNotFound()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/consultations/987654/messages", new SendMessageDto { Content = "Oi" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Send_MensagemVazia_RetornaBadRequest()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/consultations/1/messages", new SendMessageDto { Content = "" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetMessages_SemToken_RetornaUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/consultations/1/messages");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}