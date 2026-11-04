using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using ClyvoVetApi.Models;
using ClyvoVetApi.DTOs;
using ClyvoVetApi.Tests.Integration.Fixtures;
using FluentAssertions;

namespace ClyvoVetApi.Tests.Integration.Controllers;

[Collection("Integration")]
public class ConsultationsIntegrationTests
{
    private readonly CustomWebApplicationFactory _factory;

    public ConsultationsIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClient() => _factory.CreateAuthenticatedClient();

    private static async Task<(User Tutor, User Vet, Pet Pet)> SeedScenarioAsync(HttpClient client)
    {
        var tutorDto = new CreateUserDto
        {
            Name = "Tutor Consulta",
            Email = $"tutor_consulta_{Guid.NewGuid()}@test.com",
            Role = "Tutor"
        };
        var tutorResponse = await client.PostAsJsonAsync("/api/users", tutorDto);
        var tutor = (await tutorResponse.Content.ReadFromJsonAsync<User>())!;

        var vetDto = new CreateUserDto
        {
            Name = "Dr. Consulta",
            Email = $"vet_consulta_{Guid.NewGuid()}@test.com",
            Role = "Veterinario",
            Crmv = "SP-12345"
        };
        var vetResponse = await client.PostAsJsonAsync("/api/users", vetDto);
        var vet = (await vetResponse.Content.ReadFromJsonAsync<User>())!;

        var petDto = new CreatePetDto { Name = "Thor", Species = "Cachorro", TutorId = tutor.Id };
        var petResponse = await client.PostAsJsonAsync("/api/pets", petDto);
        var pet = (await petResponse.Content.ReadFromJsonAsync<Pet>())!;

        return (tutor, vet, pet);
    }

    // ==================== POST ====================

    [Fact]
    public async Task Create_DadosValidos_RetornaCreated()
    {
        // Arrange
        var client = CreateClient();
        var (_, vet, pet) = await SeedScenarioAsync(client);
        var dto = new CreateConsultationDto
        {
            PetId = pet.Id,
            VetId = vet.Id,
            ConsultationDate = DateTime.UtcNow.AddDays(2)
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/consultations", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var consultation = await response.Content.ReadFromJsonAsync<Consultation>();
        consultation!.Status.Should().Be("Agendada");
    }

    [Fact]
    public async Task Create_UsuarioNaoEVeterinario_RetornaBadRequest()
    {
        // Arrange
        var client = CreateClient();
        var (tutor, _, pet) = await SeedScenarioAsync(client);
        var dto = new CreateConsultationDto
        {
            PetId = pet.Id,
            VetId = tutor.Id, // Tutor, não veterinário
            ConsultationDate = DateTime.UtcNow.AddDays(2)
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/consultations", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_DataNoPassado_RetornaBadRequest()
    {
        // Arrange
        var client = CreateClient();
        var (_, vet, pet) = await SeedScenarioAsync(client);
        var dto = new CreateConsultationDto
        {
            PetId = pet.Id,
            VetId = vet.Id,
            ConsultationDate = DateTime.UtcNow.AddDays(-1)
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/consultations", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_ConflitoDeHorarioComMesmoVet_RetornaBadRequest()
    {
        // Arrange
        var client = CreateClient();
        var (_, vet, pet) = await SeedScenarioAsync(client);
        var date = DateTime.UtcNow.AddDays(3);

        var firstDto = new CreateConsultationDto { PetId = pet.Id, VetId = vet.Id, ConsultationDate = date };
        await client.PostAsJsonAsync("/api/consultations", firstDto);

        var conflictingDto = new CreateConsultationDto
        {
            PetId = pet.Id,
            VetId = vet.Id,
            ConsultationDate = date.AddMinutes(10) // dentro da janela mínima de 30 min
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/consultations", conflictingDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ==================== PUT ====================

    [Fact]
    public async Task Update_ConsultaJaRealizada_NaoPermiteNovaAlteracao()
    {
        // Arrange
        var client = CreateClient();
        var (_, vet, pet) = await SeedScenarioAsync(client);
        var createDto = new CreateConsultationDto
        {
            PetId = pet.Id,
            VetId = vet.Id,
            ConsultationDate = DateTime.UtcNow.AddDays(1)
        };
        var createResponse = await client.PostAsJsonAsync("/api/consultations", createDto);
        var created = (await createResponse.Content.ReadFromJsonAsync<Consultation>())!;

        var finalizeDto = new UpdateConsultationDto
        {
            ConsultationDate = created.ConsultationDate,
            Status = "Realizada"
        };
        await client.PutAsJsonAsync($"/api/consultations/{created.Id}", finalizeDto);

        var secondUpdateDto = new UpdateConsultationDto
        {
            ConsultationDate = created.ConsultationDate.AddHours(1),
            Status = "Agendada"
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/consultations/{created.Id}", secondUpdateDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ==================== DELETE ====================

    [Fact]
    public async Task Delete_ConsultaCriada_RetornaNoContent()
    {
        // Arrange
        var client = CreateClient();
        var (_, vet, pet) = await SeedScenarioAsync(client);
        var createDto = new CreateConsultationDto
        {
            PetId = pet.Id,
            VetId = vet.Id,
            ConsultationDate = DateTime.UtcNow.AddDays(1)
        };
        var createResponse = await client.PostAsJsonAsync("/api/consultations", createDto);
        var created = (await createResponse.Content.ReadFromJsonAsync<Consultation>())!;

        // Act
        var response = await client.DeleteAsync($"/api/consultations/{created.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}

