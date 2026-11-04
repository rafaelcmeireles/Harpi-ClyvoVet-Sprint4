using System.Net;
using ClyvoVetApi.Tests.Integration.Fixtures;
using FluentAssertions;

namespace ClyvoVetApi.Tests.Integration;

[Collection("Integration")]
public class HealthCheckIntegrationTests
{
    private readonly HttpClient _client;

    public HealthCheckIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthLive_Endpoint_RetornaOk()
    {
        // Arrange — sem configuração adicional (apenas API online)

        // Act
        var response = await _client.GetAsync("/health/live");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Health_EndpointGeral_RetornaJson()
    {
        // Arrange

        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task HealthReady_SemServicoExternoConfigurado_RetornaOk()
    {
        // Arrange — em ambiente de testes o Oracle é substituído por InMemory (não é checado)
        // e nenhuma URL de serviço externo é configurada, então o check "external-services" é Healthy.

        // Act
        var response = await _client.GetAsync("/health/ready");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
