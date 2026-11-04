using ClyvoVetApi.Domain;
using FluentAssertions;

namespace ClyvoVetApi.Tests.Unit.Domain;

public class ConsultationStatusesTests
{
    [Theory]
    [InlineData("Agendada")]
    [InlineData("Realizada")]
    [InlineData("cancelada")]
    public void IsValid_StatusConhecido_RetornaTrue(string status)
    {
        // Arrange & Act
        var result = ConsultationStatuses.IsValid(status);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("Finalizada")]
    [InlineData("")]
    [InlineData(null)]
    public void IsValid_StatusDesconhecido_RetornaFalse(string? status)
    {
        // Arrange & Act
        var result = ConsultationStatuses.IsValid(status);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("Agendada", "Realizada")]
    [InlineData("Agendada", "Cancelada")]
    [InlineData("Agendada", "Agendada")]
    public void CanTransition_DeAgendadaParaStatusValido_RetornaTrue(string current, string next)
    {
        // Arrange & Act
        var result = ConsultationStatuses.CanTransition(current, next);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("Realizada", "Agendada")]
    [InlineData("Realizada", "Realizada")]
    [InlineData("Cancelada", "Agendada")]
    [InlineData("Cancelada", "Cancelada")]
    public void CanTransition_DeStatusFinal_RetornaFalse(string current, string next)
    {
        // Arrange & Act
        var result = ConsultationStatuses.CanTransition(current, next);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void CanTransition_StatusInvalido_RetornaFalse()
    {
        // Arrange & Act
        var result = ConsultationStatuses.CanTransition("Agendada", "Finalizada");

        // Assert
        result.Should().BeFalse();
    }
}
