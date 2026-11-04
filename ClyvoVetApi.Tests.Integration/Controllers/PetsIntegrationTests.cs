using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using ClyvoVetApi.Models;
using ClyvoVetApi.DTOs;
using ClyvoVetApi.Tests.Integration.Fixtures;
using FluentAssertions;

namespace ClyvoVetApi.Tests.Integration.Controllers;

[Collection("Integration")]
public class PetsIntegrationTests
{
    private readonly CustomWebApplicationFactory _factory;

    public PetsIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClient() => _factory.CreateAuthenticatedClient();

    private static async Task<User> CreateTutorAsync(HttpClient client)
    {
        var dto = new CreateUserDto
        {
            Name = "Tutor Pets",
            Email = $"tutor_pets_{Guid.NewGuid()}@test.com",
            Role = "Tutor"
        };
        var response = await client.PostAsJsonAsync("/api/users", dto);
        return (await response.Content.ReadFromJsonAsync<User>())!;
    }

    // ==================== POST ====================

    [Fact]
    public async Task Create_TutorValido_RetornaCreated()
    {
        // Arrange
        var client = CreateClient();
        var tutor = await CreateTutorAsync(client);
        var dto = new CreatePetDto
        {
            Name = "Rex",
            Species = "Cachorro",
            Breed = "Labrador",
            WeightKg = 28.5,
            TutorId = tutor.Id
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/pets", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var pet = await response.Content.ReadFromJsonAsync<Pet>();
        pet!.TutorId.Should().Be(tutor.Id);
    }

    [Fact]
    public async Task Create_TutorInexistente_RetornaBadRequest()
    {
        // Arrange
        var client = CreateClient();
        var dto = new CreatePetDto { Name = "Rex", Species = "Cachorro", TutorId = 999999 };

        // Act
        var response = await client.PostAsJsonAsync("/api/pets", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_DataNascimentoFutura_RetornaBadRequest()
    {
        // Arrange
        var client = CreateClient();
        var tutor = await CreateTutorAsync(client);
        var dto = new CreatePetDto
        {
            Name = "Rex",
            Species = "Cachorro",
            TutorId = tutor.Id,
            BirthDate = DateTime.UtcNow.AddDays(5)
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/pets", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_PesoInvalido_RetornaBadRequest()
    {
        // Arrange
        var client = CreateClient();
        var tutor = await CreateTutorAsync(client);
        var dto = new CreatePetDto
        {
            Name = "Rex",
            Species = "Cachorro",
            TutorId = tutor.Id,
            WeightKg = -2
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/pets", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ==================== GET ====================

    [Fact]
    public async Task GetById_PetInexistente_RetornaNotFound()
    {
        // Arrange
        var client = CreateClient();

        // Act
        var response = await client.GetAsync("/api/pets/999999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetByTutor_TutorComPet_RetornaListaComPet()
    {
        // Arrange
        var client = CreateClient();
        var tutor = await CreateTutorAsync(client);
        var dto = new CreatePetDto { Name = "Bolinha", Species = "Cachorro", TutorId = tutor.Id };
        await client.PostAsJsonAsync("/api/pets", dto);

        // Act
        var response = await client.GetAsync($"/api/pets/tutor/{tutor.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var pets = await response.Content.ReadFromJsonAsync<List<Pet>>();
        pets!.Should().Contain(p => p.Name == "Bolinha");
    }

    // ==================== DELETE ====================

    [Fact]
    public async Task Delete_PetCriado_RetornaNoContent()
    {
        // Arrange
        var client = CreateClient();
        var tutor = await CreateTutorAsync(client);
        var dto = new CreatePetDto { Name = "Apagar", Species = "Gato", TutorId = tutor.Id };
        var postResponse = await client.PostAsJsonAsync("/api/pets", dto);
        var created = await postResponse.Content.ReadFromJsonAsync<Pet>();

        // Act
        var response = await client.DeleteAsync($"/api/pets/{created!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
