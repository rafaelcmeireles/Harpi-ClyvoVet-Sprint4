using System.Net;
using System.Net.Http.Json;
using ClyvoVetApi.DTOs;
using ClyvoVetApi.Models;
using ClyvoVetApi.Tests.Integration.Fixtures;
using FluentAssertions;

namespace ClyvoVetApi.Tests.Integration.Controllers;

[Collection("Integration")]
public class PagingAndHateoasIntegrationTests
{
    private readonly CustomWebApplicationFactory _factory;

    public PagingAndHateoasIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<string> SeedTutorsAsync(HttpClient client, int count)
    {
        var marker = $"Paginado{Guid.NewGuid():N}"[..16];
        for (var i = 1; i <= count; i++)
        {
            var response = await client.PostAsJsonAsync("/api/users", new CreateUserDto
            {
                Name = $"{marker} {i:D2}", Email = $"{marker}_{i}@test.com", Role = "Tutor"
            });
            response.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        return marker;
    }

    [Fact]
    public async Task GetAll_ComPaginacao_RetornaPaginaEMetadados()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient();
        var marker = await SeedTutorsAsync(client, 5);

        // Act
        var response = await client.GetAsync($"/api/users?search={marker}&page=2&pageSize=2&sortBy=name");
        var page = await response.Content.ReadFromJsonAsync<PagedResponse<User>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        page!.Page.Should().Be(2);
        page.PageSize.Should().Be(2);
        page.TotalItems.Should().Be(5);
        page.TotalPages.Should().Be(3);
        page.Items.Select(i => i.Data.Name).Should().Equal($"{marker} 03", $"{marker} 04");
    }

    [Fact]
    public async Task GetAll_OrdenacaoDecrescente_InverteAOrdem()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient();
        var marker = await SeedTutorsAsync(client, 3);

        // Act
        var page = await client.GetFromJsonAsync<PagedResponse<User>>(
            $"/api/users?search={marker}&sortBy=name&desc=true");

        // Assert
        page!.Items.Select(i => i.Data.Name).Should().Equal($"{marker} 03", $"{marker} 02", $"{marker} 01");
    }

    [Fact]
    public async Task GetAll_FiltroPorRole_RetornaApenasDoPapelInformado()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient();
        await SeedTutorsAsync(client, 2);

        // Act
        var page = await client.GetFromJsonAsync<PagedResponse<User>>("/api/users?role=Tutor&pageSize=100");

        // Assert
        page!.Items.Should().NotBeEmpty();
        page.Items.Should().OnlyContain(i => i.Data.Role == "Tutor");
    }

    [Fact]
    public async Task GetAll_PaginaIntermediaria_TrazLinksDeNavegacao()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient();
        var marker = await SeedTutorsAsync(client, 5);

        // Act
        var page = await client.GetFromJsonAsync<PagedResponse<User>>($"/api/users?search={marker}&page=2&pageSize=2");

        // Assert
        page!.Links.Select(l => l.Rel).Should().BeEquivalentTo(new[] { "self", "first", "prev", "next", "last" });
        page.Links.Single(l => l.Rel == "next").Href.Should().Contain("page=3");
        page.Links.Single(l => l.Rel == "prev").Href.Should().Contain("page=1");
    }

    [Fact]
    public async Task GetAll_CadaItemTrazLinksHateoas()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient();
        var marker = await SeedTutorsAsync(client, 1);

        // Act
        var page = await client.GetFromJsonAsync<PagedResponse<User>>($"/api/users?search={marker}");
        var item = page!.Items.Single();

        // Assert
        item.Links.Select(l => l.Rel).Should().Contain(new[] { "self", "update", "delete", "pets" });
        item.Links.Single(l => l.Rel == "self").Href.Should().EndWith($"/api/users/{item.Data.Id}");
    }

    [Fact]
    public async Task GetById_RetornaRecursoComLinks()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient();
        var created = await (await client.PostAsJsonAsync("/api/users", new CreateUserDto
        {
            Name = "Link Vet", Email = $"linkvet_{Guid.NewGuid()}@test.com", Role = "Veterinario", Crmv = "SP-1234"
        })).Content.ReadFromJsonAsync<User>();

        // Act
        var resource = await client.GetFromJsonAsync<Resource<User>>($"/api/users/{created!.Id}");

        // Assert
        resource!.Data.Id.Should().Be(created.Id);
        resource.Links.Select(l => l.Rel).Should().Contain("consultations");
    }

    [Fact]
    public async Task GetAll_CampoDeOrdenacaoInvalido_RetornaBadRequest()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient();

        // Act
        var response = await client.GetAsync("/api/users?sortBy=passwordHash");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("page=0")]
    [InlineData("pageSize=0")]
    [InlineData("pageSize=101")]
    public async Task GetAll_ParametrosDePaginacaoInvalidos_RetornaBadRequest(string query)
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient();

        // Act
        var response = await client.GetAsync($"/api/users?{query}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetAllPets_FiltroPorTutor_RetornaSomentePetsDoTutor()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient();
        var tutor = await (await client.PostAsJsonAsync("/api/users", new CreateUserDto
        {
            Name = "Tutor Pets", Email = $"tp_{Guid.NewGuid()}@test.com", Role = "Tutor"
        })).Content.ReadFromJsonAsync<User>();
        foreach (var name in new[] { "Rex", "Mel" })
            await client.PostAsJsonAsync("/api/pets", new CreatePetDto { Name = name, Species = "Cachorro", TutorId = tutor!.Id });

        // Act
        var page = await client.GetFromJsonAsync<PagedResponse<Pet>>($"/api/pets?tutorId={tutor!.Id}&sortBy=name");

        // Assert
        page!.TotalItems.Should().Be(2);
        page.Items.Select(i => i.Data.Name).Should().Equal("Mel", "Rex");
        page.Items.First().Links.Select(l => l.Rel).Should().Contain(new[] { "tutor", "consultations" });
    }
}