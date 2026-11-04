namespace ClyvoVetApi.Tests.Integration.Fixtures;

/// <summary>
/// Collection Fixture que compartilha uma única instância de <see cref="CustomWebApplicationFactory"/>
/// (e, portanto, do mesmo banco InMemory) entre todas as classes de teste de integração,
/// evitando subir um host ASP.NET Core novo para cada classe. Seguro porque nenhum teste
/// depende de o banco estar vazio — cada um cria e verifica apenas os próprios dados.
/// </summary>
[CollectionDefinition("Integration")]
public class IntegrationTestCollection : ICollectionFixture<CustomWebApplicationFactory>
{
    // Classe marcadora - não contém código.
}
