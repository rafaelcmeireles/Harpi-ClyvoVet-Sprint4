using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ClyvoVetApi.DTOs;
using ClyvoVetApi.Models;
using ClyvoVetApi.Tests.Integration.Fixtures;
using FluentAssertions;

namespace ClyvoVetApi.Tests.Integration.Controllers;

[Collection("Integration")]
public class AuthIntegrationTests
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("/api/users")]
    [InlineData("/api/pets")]
    [InlineData("/api/consultations")]
    public async Task Get_SemToken_RetornaUnauthorized(string url)
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync(url);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_ComTokenInvalido_RetornaUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "token.invalido.aqui");

        // Act
        var response = await client.GetAsync("/api/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_CredenciaisValidas_RetornaTokenQueAcessaEndpointProtegido()
    {
        // Arrange - cadastro anônimo com senha
        var client = _factory.CreateClient();
        var email = $"login_{Guid.NewGuid()}@test.com";
        var register = await client.PostAsJsonAsync("/api/users", new CreateUserDto
        {
            Name = "Login Tutor", Email = email, Role = "Tutor", Password = "senha123"
        });
        register.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act
        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginDto { Email = email, Password = "senha123" });
        var token = await login.Content.ReadFromJsonAsync<TokenResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!.AccessToken);
        var protectedResponse = await client.GetAsync("/api/users");

        // Assert
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        token.AccessToken.Should().NotBeNullOrEmpty();
        protectedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_SenhaIncorreta_RetornaUnauthorized()
    {
        // Arrange
        var client = _factory.CreateClient();
        var email = $"wrong_{Guid.NewGuid()}@test.com";
        await client.PostAsJsonAsync("/api/users", new CreateUserDto
        {
            Name = "Wrong", Email = email, Role = "Tutor", Password = "senha123"
        });

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginDto { Email = email, Password = "errada" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_ComSenha_NaoExpoeHashNaResposta()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/users", new CreateUserDto
        {
            Name = "Seguro", Email = $"seg_{Guid.NewGuid()}@test.com", Role = "Tutor", Password = "senha123"
        });
        var body = await response.Content.ReadAsStringAsync();

        // Assert
        body.ToLowerInvariant().Should().NotContain("passwordhash").And.NotContain("senha123");
    }

    [Fact]
    public async Task DeleteConsulta_ComoTutor_RetornaForbidden()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(role: "Tutor");

        // Act
        var response = await client.DeleteAsync("/api/consultations/1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}