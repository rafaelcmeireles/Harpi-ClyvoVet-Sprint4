using ClyvoVetApi.Application.Abstractions;
using Moq;

namespace ClyvoVetApi.Tests.Unit.Application.Fixtures;

/// <summary>
/// Fixture compartilhada entre as classes de teste da camada de Aplicação: fornece um
/// <see cref="IDateTimeProvider"/> com data fixa, garantindo testes determinísticos
/// (sem depender de <c>DateTime.UtcNow</c> real) e evitando recriar o mock em cada classe.
/// </summary>
public class FixedClockFixture
{
    public DateTime FixedUtcNow { get; } = new(2026, 9, 13, 12, 0, 0, DateTimeKind.Utc);

    public IDateTimeProvider Clock { get; }

    public FixedClockFixture()
    {
        var mock = new Mock<IDateTimeProvider>();
        mock.Setup(c => c.UtcNow).Returns(FixedUtcNow);
        Clock = mock.Object;
    }
}

/// <summary>
/// Collection Fixture: agrupa as classes de teste da camada de Aplicação para que todas
/// compartilhem a mesma instância de <see cref="FixedClockFixture"/>, em vez de o xUnit
/// criar (e descartar) uma nova a cada classe.
/// </summary>
[CollectionDefinition("ApplicationServices")]
public class ApplicationServicesCollection : ICollectionFixture<FixedClockFixture>
{
    // Classe marcadora - não contém código. Apenas associa a fixture à collection.
}
