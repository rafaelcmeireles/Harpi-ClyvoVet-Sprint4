using ClyvoVetApi.Domain;
using FluentAssertions;

namespace ClyvoVetApi.Tests.Unit.Domain;

public class UserValidationRulesTests
{
    [Fact]
    public void RequiresCrmv_Veterinario_RetornaTrue()
    {
        // Arrange & Act
        var result = UserValidationRules.RequiresCrmv("Veterinario");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void RequiresCrmv_Tutor_RetornaFalse()
    {
        // Arrange & Act
        var result = UserValidationRules.RequiresCrmv("Tutor");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void HasValidCrmv_VeterinarioComCrmv_RetornaTrue()
    {
        // Arrange & Act
        var result = UserValidationRules.HasValidCrmv("Veterinario", "CRMV-SP-12345");

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void HasValidCrmv_VeterinarioSemCrmv_RetornaFalse(string? crmv)
    {
        // Arrange & Act
        var result = UserValidationRules.HasValidCrmv("Veterinario", crmv);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void HasValidCrmv_TutorSemCrmv_RetornaTrue()
    {
        // Arrange & Act
        var result = UserValidationRules.HasValidCrmv("Tutor", null);

        // Assert
        result.Should().BeTrue();
    }
}
