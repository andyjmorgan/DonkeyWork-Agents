using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using DonkeyWork.Agents.Actions.Contracts.Attributes;
using DonkeyWork.Agents.Actions.Contracts.Models;
using DonkeyWork.Agents.Common.Sdk.Types;

namespace DonkeyWork.Agents.Actions.Core.Tests.Models;

public class BaseActionParametersTests
{
    private class TestParameters : BaseActionParameters
    {
        [Required]
        public string RequiredField { get; set; } = string.Empty;

        [Range(1, 100)]
        public int NumberField { get; set; }

        [Range(1, 100)]
        public Resolvable<int> ResolvableNumber { get; set; } = 50;

        public override (bool valid, List<ValidationResult> results) IsValid()
        {
            return ValidateDataAnnotations();
        }
    }

    [Fact]
    public void IsValid_WithValidParameters_ReturnsTrue()
    {
        // Arrange
        var parameters = new TestParameters
        {
            RequiredField = "test",
            NumberField = 50,
            ResolvableNumber = 75
        };

        // Act
        var (valid, results) = parameters.IsValid();

        // Assert
        Assert.True(valid);
        Assert.Empty(results);
    }

    [Fact]
    public void IsValid_WithMissingRequiredField_ReturnsFalse()
    {
        // Arrange
        var parameters = new TestParameters
        {
            RequiredField = "",
            NumberField = 50
        };

        // Act
        var (valid, results) = parameters.IsValid();

        // Assert
        Assert.False(valid);
        Assert.Single(results);
        Assert.Contains("RequiredField", results[0].MemberNames);
    }

    [Fact]
    public void IsValid_WithNumberOutOfRange_ReturnsFalse()
    {
        // Arrange
        var parameters = new TestParameters
        {
            RequiredField = "test",
            NumberField = 150 // Out of range (1-100)
        };

        // Act
        var (valid, results) = parameters.IsValid();

        // Assert
        Assert.False(valid);
        Assert.Single(results);
        Assert.Contains("NumberField", results[0].MemberNames);
    }

    [Fact]
    public void IsValid_WithResolvableLiteralInRange_ReturnsTrue()
    {
        // Arrange
        var parameters = new TestParameters
        {
            RequiredField = "test",
            NumberField = 50,
            ResolvableNumber = 75 // Literal value in range
        };

        // Act
        var (valid, results) = parameters.IsValid();

        // Assert
        Assert.True(valid);
        Assert.Empty(results);
    }

    [Fact]
    public void IsValid_WithResolvableLiteralOutOfRange_ReturnsFalse()
    {
        // Arrange
        var parameters = new TestParameters
        {
            RequiredField = "test",
            NumberField = 50,
            ResolvableNumber = 200 // Literal value out of range
        };

        // Act
        var (valid, results) = parameters.IsValid();

        // Assert
        Assert.False(valid);
        Assert.Single(results);
        Assert.Contains("ResolvableNumber", results[0].MemberNames);
    }

    [Fact]
    public void IsValid_WithResolvableExpression_SkipsValidation()
    {
        // Arrange
        var parameters = new TestParameters
        {
            RequiredField = "test",
            NumberField = 50,
            ResolvableNumber = "{{Variables.count}}" // Expression, validation skipped
        };

        // Act
        var (valid, results) = parameters.IsValid();

        // Assert
        Assert.True(valid);
        Assert.Empty(results);
    }

    [Fact]
    public void IsValid_WithMultipleErrors_ReturnsAllErrors()
    {
        // Arrange
        var parameters = new TestParameters
        {
            RequiredField = "", // Missing
            NumberField = 200, // Out of range
            ResolvableNumber = 300 // Out of range
        };

        // Act
        var (valid, results) = parameters.IsValid();

        // Assert
        Assert.False(valid);
        Assert.Equal(3, results.Count);
    }

    [Fact]
    public void Version_DefaultValue_Is1_0()
    {
        // Arrange & Act
        var parameters = new TestParameters
        {
            RequiredField = "test"
        };

        // Assert
        Assert.Equal("1.0", parameters.Version);
    }
}
