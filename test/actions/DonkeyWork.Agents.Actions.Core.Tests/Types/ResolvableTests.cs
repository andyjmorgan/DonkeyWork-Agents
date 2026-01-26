using DonkeyWork.Agents.Actions.Contracts.Types;

namespace DonkeyWork.Agents.Actions.Core.Tests.Types;

public class ResolvableTests
{
    [Fact]
    public void Constructor_WithLiteralValue_StoresValueAsString()
    {
        // Arrange & Act
        var resolvable = new Resolvable<int>(42);

        // Assert
        Assert.Equal("42", resolvable.RawValue);
        Assert.False(resolvable.IsExpression);
    }

    [Fact]
    public void Constructor_WithExpression_StoresExpressionString()
    {
        // Arrange & Act
        var resolvable = new Resolvable<int>("{{Variables.timeout}}");

        // Assert
        Assert.Equal("{{Variables.timeout}}", resolvable.RawValue);
        Assert.True(resolvable.IsExpression);
    }

    [Fact]
    public void ImplicitConversion_FromInt_CreatesResolvable()
    {
        // Arrange & Act
        Resolvable<int> resolvable = 100;

        // Assert
        Assert.Equal("100", resolvable.RawValue);
        Assert.False(resolvable.IsExpression);
    }

    [Fact]
    public void ImplicitConversion_FromString_CreatesResolvable()
    {
        // Arrange & Act
        Resolvable<int> resolvable = "{{Nodes.step1.output}}";

        // Assert
        Assert.Equal("{{Nodes.step1.output}}", resolvable.RawValue);
        Assert.True(resolvable.IsExpression);
    }

    [Fact]
    public void IsExpression_WithoutBraces_ReturnsFalse()
    {
        // Arrange & Act
        var resolvable = new Resolvable<string>("plain text");

        // Assert
        Assert.False(resolvable.IsExpression);
    }

    [Fact]
    public void IsExpression_WithSingleBrace_ReturnsFalse()
    {
        // Arrange & Act
        var resolvable1 = new Resolvable<string>("{{missing end");
        var resolvable2 = new Resolvable<string>("missing start}}");

        // Assert
        Assert.False(resolvable1.IsExpression);
        Assert.False(resolvable2.IsExpression);
    }

    [Fact]
    public void IsExpression_WithBothBraces_ReturnsTrue()
    {
        // Arrange & Act
        var resolvable = new Resolvable<string>("{{expression}}");

        // Assert
        Assert.True(resolvable.IsExpression);
    }

    [Fact]
    public void IsPureExpression_WithOnlyExpression_ReturnsTrue()
    {
        // Arrange & Act
        var resolvable = new Resolvable<string>("{{Variables.name}}");

        // Assert
        Assert.True(resolvable.IsPureExpression);
    }

    [Fact]
    public void IsPureExpression_WithTextAndExpression_ReturnsFalse()
    {
        // Arrange & Act
        var resolvable = new Resolvable<string>("Hello {{Variables.name}}!");

        // Assert
        Assert.False(resolvable.IsPureExpression);
    }

    [Fact]
    public void Constructor_WithBooleanValue_StoresAsString()
    {
        // Arrange & Act
        var resolvable = new Resolvable<bool>(true);

        // Assert
        Assert.Equal("true", resolvable.RawValue);
        Assert.False(resolvable.IsExpression);
    }

    [Fact]
    public void Constructor_WithDecimalValue_StoresAsString()
    {
        // Arrange & Act
        var resolvable = new Resolvable<decimal>(123.45m);

        // Assert
        Assert.Equal("123.45", resolvable.RawValue);
        Assert.False(resolvable.IsExpression);
    }

    [Fact]
    public void Constructor_WithEmptyString_StoresEmptyString()
    {
        // Arrange & Act
        var resolvable = new Resolvable<string>("");

        // Assert
        Assert.Equal("", resolvable.RawValue);
        Assert.False(resolvable.IsExpression);
    }

    [Fact]
    public void ToString_ReturnsRawValue()
    {
        // Arrange
        var resolvable = new Resolvable<int>("{{Variables.count}}");

        // Act
        var result = resolvable.ToString();

        // Assert
        Assert.Equal("{{Variables.count}}", result);
    }
}
