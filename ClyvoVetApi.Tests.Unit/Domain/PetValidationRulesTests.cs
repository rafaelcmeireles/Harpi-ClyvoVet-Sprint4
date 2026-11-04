using ClyvoVetApi.Domain;
using FluentAssertions;

namespace ClyvoVetApi.Tests.Unit.Domain;

public class PetValidationRulesTests
{
    private static readonly DateTime ReferenceNow = new(2026, 9, 13);

    [Fact]
    public void IsBirthDateValid_DataNoPassado_RetornaTrue()
    {
        // Arrange & Act
        var result = PetValidationRules.IsBirthDateValid(ReferenceNow.AddYears(-2), ReferenceNow);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsBirthDateValid_DataDeHoje_RetornaTrue()
    {
        // Arrange & Act
        var result = PetValidationRules.IsBirthDateValid(ReferenceNow, ReferenceNow);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsBirthDateValid_DataFutura_RetornaFalse()
    {
        // Arrange & Act
        var result = PetValidationRules.IsBirthDateValid(ReferenceNow.AddDays(1), ReferenceNow);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsBirthDateValid_SemDataInformada_RetornaTrue()
    {
        // Arrange & Act
        var result = PetValidationRules.IsBirthDateValid(null, ReferenceNow);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(0.1)]
    [InlineData(28.5)]
    public void IsWeightValid_PesoPositivo_RetornaTrue(double weight)
    {
        // Arrange & Act
        var result = PetValidationRules.IsWeightValid(weight);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void IsWeightValid_PesoZeroOuNegativo_RetornaFalse(double weight)
    {
        // Arrange & Act
        var result = PetValidationRules.IsWeightValid(weight);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsWeightValid_SemPesoInformado_RetornaTrue()
    {
        // Arrange & Act
        var result = PetValidationRules.IsWeightValid(null);

        // Assert
        result.Should().BeTrue();
    }
}
