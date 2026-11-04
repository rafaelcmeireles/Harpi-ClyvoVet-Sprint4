using ClyvoVetApi.Domain;
using FluentAssertions;

namespace ClyvoVetApi.Tests.Unit.Domain;

public class UserRolesTests
{
    [Theory]
    [InlineData("Tutor")]
    [InlineData("tutor")]
    [InlineData("Veterinario")]
    [InlineData("VETERINARIO")]
    public void IsValid_RoleReconhecido_RetornaTrue(string role)
    {
        // Arrange & Act
        var result = UserRoles.IsValid(role);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("")]
    [InlineData(null)]
    public void IsValid_RoleDesconhecido_RetornaFalse(string? role)
    {
        // Arrange & Act
        var result = UserRoles.IsValid(role);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsTutor_RoleTutorEmQualquerCaixa_RetornaTrue()
    {
        // Arrange & Act
        var result = UserRoles.IsTutor("tutor");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsVeterinario_RoleTutor_RetornaFalse()
    {
        // Arrange & Act
        var result = UserRoles.IsVeterinario("Tutor");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsVeterinario_RoleVeterinario_RetornaTrue()
    {
        // Arrange & Act
        var result = UserRoles.IsVeterinario("Veterinario");

        // Assert
        result.Should().BeTrue();
    }
}
