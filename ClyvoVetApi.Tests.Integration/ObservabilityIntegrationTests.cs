using System.Linq;
using System.Net;
using System.Net.Http;
using ClyvoVetApi.Tests.Integration.Fixtures;
using FluentAssertions;

namespace ClyvoVetApi.Tests.Integration;

[Collection("Integration")]
public class ObservabilityIntegrationTests
{
    private readonly CustomWebApplicationFactory _factory;

    public ObservabilityIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_SemHeaderDeCorrelacao_ApiGeraUmNovo()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health/live");

        // Assert
        response.Headers.Contains("X-Correlation-Id").Should().BeTrue();
        response.Headers.GetValues("X-Correlation-Id").First().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Get_ComHeaderDeCorrelacao_ApiEcoaOMesmoId()
    {
        // Arrange
        var client = _factory.CreateClient();
        var correlationId = Guid.NewGuid().ToString();
        var request = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        request.Headers.Add("X-Correlation-Id", correlationId);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.Headers.GetValues("X-Correlation-Id").First().Should().Be(correlationId);
    }

    [Fact]
    public async Task Get_SwaggerJson_RetornaDocumentoValido()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/swagger/v1/swagger.json");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task Get_Raiz_RetornaSwaggerUi()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
