using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Headers;
using ClyvoVetApi.Application.Abstractions;
using ClyvoVetApi.Data;
using ClyvoVetApi.Models;

namespace ClyvoVetApi.Tests.Integration.Fixtures;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    // Nome fixo por instancia da factory para garantir banco compartilhado entre requests
    private readonly string _dbName = "ClyvoVetIntegrationTest_" + Guid.NewGuid().ToString();

    /// <summary>
    /// Cria um HttpClient já autenticado com um JWT válido (emitido pelo mesmo ITokenService da API).
    /// </summary>
    public HttpClient CreateAuthenticatedClient(string role = "Veterinario", int userId = 9999)
    {
        var client = CreateClient();

        using var scope = Services.CreateScope();
        var token = scope.ServiceProvider.GetRequiredService<ITokenService>().Generate(new User
        {
            Id = userId,
            Name = "Usuario de Teste",
            Email = "teste@clyvovet.com",
            Role = role
        });

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        return client;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Remove todos os registros relacionados ao DbContext
            var toRemove = services
                .Where(d =>
                    d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                    d.ServiceType == typeof(DbContextOptions) ||
                    d.ServiceType == typeof(AppDbContext))
                .ToList();

            foreach (var d in toRemove)
                services.Remove(d);

            // Registra DbContext com InMemory usando nome fixo da instancia
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));

            // Cria e inicializa o banco InMemory
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
        });
    }
}
