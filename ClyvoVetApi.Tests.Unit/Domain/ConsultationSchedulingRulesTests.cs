using ClyvoVetApi.Domain;
using FluentAssertions;

namespace ClyvoVetApi.Tests.Unit.Domain;

public class ConsultationSchedulingRulesTests
{
    private static readonly DateTime Now = new(2026, 9, 13, 12, 0, 0);

    [Fact]
    public void IsFutureOrPresent_DataFutura_RetornaTrue()
    {
        // Arrange & Act
        var result = ConsultationSchedulingRules.IsFutureOrPresent(Now.AddDays(1), Now);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsFutureOrPresent_MesmaData_RetornaTrue()
    {
        // Arrange & Act
        var result = ConsultationSchedulingRules.IsFutureOrPresent(Now, Now);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsFutureOrPresent_DataPassada_RetornaFalse()
    {
        // Arrange & Act
        var result = ConsultationSchedulingRules.IsFutureOrPresent(Now.AddMinutes(-1), Now);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void HasConflict_HorariosDentroDaJanelaMinima_RetornaTrue()
    {
        // Arrange
        var existing = Now;
        var newDate = Now.AddMinutes(10);

        // Act
        var result = ConsultationSchedulingRules.HasConflict(existing, newDate, TimeSpan.FromMinutes(30));

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void HasConflict_HorariosForaDaJanelaMinima_RetornaFalse()
    {
        // Arrange
        var existing = Now;
        var newDate = Now.AddHours(2);

        // Act
        var result = ConsultationSchedulingRules.HasConflict(existing, newDate, TimeSpan.FromMinutes(30));

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void HasConflict_ExatamenteNoLimiteDaJanela_RetornaFalse()
    {
        // Arrange
        var existing = Now;
        var newDate = Now.AddMinutes(30);

        // Act
        var result = ConsultationSchedulingRules.HasConflict(existing, newDate, TimeSpan.FromMinutes(30));

        // Assert
        result.Should().BeFalse();
    }
}
