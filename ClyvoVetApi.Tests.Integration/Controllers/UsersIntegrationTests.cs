using System.Net;
using System.Net.Http.Json;
using ClyvoVetApi.Models;
using ClyvoVetApi.DTOs;
using ClyvoVetApi.Tests.Integration.Fixtures;
using FluentAssertions;

namespace ClyvoVetApi.Tests.Integration.Controllers;

[Collection("Integration")]
public class UsersIntegrationTests
{
    private readonly CustomWebApplicationFactory _factory;

    public UsersIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClient() => _factory.CreateAuthenticatedClient();

    // ==================== GET /api/users ====================

    [Fact]
    public async Task GetAll_SemUsuarios_RetornaOkComListaVazia()
    {
        // Arrange
        var client = CreateClient();

        // Act
        var response = await client.GetAsync("/api/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<PagedResponse<User>>();
        page.Should().NotBeNull();
        page!.Items.Should().NotBeNull();
        page.Links.Should().Contain(l => l.Rel == "self");
    }

    // ==================== POST /api/users ====================

    [Fact]
    public async Task Create_DadosValidos_RetornaCreated()
    {
        // Arrange
        var client = CreateClient();
        var dto = new CreateUserDto
        {
            Name = "Maria Silva",
            Email = $"maria_{Guid.NewGuid()}@test.com",
            Role = "Tutor",
            Phone = "(11) 99999-0000"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/users", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var user = await response.Content.ReadFromJsonAsync<User>();
        user!.Name.Should().Be("Maria Silva");
        user.Role.Should().Be("Tutor");
    }

    [Fact]
    public async Task Create_RoleInvalida_RetornaBadRequest()
    {
        // Arrange
        var client = CreateClient();
        var dto = new CreateUserDto
        {
            Name = "Teste",
            Email = $"teste_{Guid.NewGuid()}@test.com",
            Role = "Admin"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/users", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ==================== GET /api/users/{id} ====================

    [Fact]
    public async Task GetById_UsuarioCriado_RetornaOkComUsuario()
    {
        // Arrange — criar e buscar com o mesmo client (mesmo banco)
        var client = CreateClient();
        var dto = new CreateUserDto
        {
            Name = "Carlos",
            Email = $"carlos_{Guid.NewGuid()}@test.com",
            Role = "Veterinario",
            Crmv = "SP-12345"
        };
        var postResponse = await client.PostAsJsonAsync("/api/users", dto);
        var created = await postResponse.Content.ReadFromJsonAsync<User>();

        // Act
        var response = await client.GetAsync($"/api/users/{created!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var resource = await response.Content.ReadFromJsonAsync<Resource<User>>();
        resource!.Data.Email.Should().Be(dto.Email);
        resource.Links.Should().Contain(l => l.Rel == "self");
    }

    [Fact]
    public async Task GetById_UsuarioInexistente_RetornaNotFound()
    {
        // Arrange
        var client = CreateClient();

        // Act
        var response = await client.GetAsync("/api/users/99999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ==================== PUT /api/users/{id} ====================

    [Fact]
    public async Task Update_UsuarioExistente_RetornaOkAtualizado()
    {
        // Arrange
        var client = CreateClient();
        var email = $"original_{Guid.NewGuid()}@test.com";
        var createDto = new CreateUserDto
        {
            Name = "Original",
            Email = email,
            Role = "Tutor"
        };
        var postResponse = await client.PostAsJsonAsync("/api/users", createDto);
        var created = await postResponse.Content.ReadFromJsonAsync<User>();

        var updateDto = new UpdateUserDto { Name = "Atualizado", Email = email };

        // Act
        var response = await client.PutAsJsonAsync($"/api/users/{created!.Id}", updateDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<User>();
        updated!.Name.Should().Be("Atualizado");
    }

    // ==================== DELETE /api/users/{id} ====================

    [Fact]
    public async Task Delete_UsuarioCriado_RetornaNoContent()
    {
        // Arrange
        var client = CreateClient();
        var dto = new CreateUserDto
        {
            Name = "Para Deletar",
            Email = $"delete_{Guid.NewGuid()}@test.com",
            Role = "Tutor"
        };
        var postResponse = await client.PostAsJsonAsync("/api/users", dto);
        var created = await postResponse.Content.ReadFromJsonAsync<User>();

        // Act
        var response = await client.DeleteAsync($"/api/users/{created!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verificar que foi mesmo deletado
        var getResponse = await client.GetAsync($"/api/users/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

