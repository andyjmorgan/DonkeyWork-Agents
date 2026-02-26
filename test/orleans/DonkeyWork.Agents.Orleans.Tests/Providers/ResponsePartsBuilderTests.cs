using System.Text.Json;
using DonkeyWork.Agents.Orleans.Core.Providers;
using Xunit;

namespace DonkeyWork.Agents.Orleans.Tests.Providers;

public class ResponsePartsBuilderTests
{
    #region AppendText Tests

    [Fact]
    public void AppendText_AccumulatesText()
    {
        // Arrange
        var builder = new ResponsePartsBuilder();

        // Act
        builder.AppendText("Hello ");
        builder.AppendText("World");

        // Assert
        Assert.Equal("Hello World", builder.AccumulatedText);
    }

    [Fact]
    public void AppendText_WithEmptyString_DoesNotFail()
    {
        // Arrange
        var builder = new ResponsePartsBuilder();

        // Act
        builder.AppendText("");

        // Assert
        Assert.Equal("", builder.AccumulatedText);
    }

    #endregion

    #region AppendThinking Tests

    [Fact]
    public void AppendThinking_AccumulatesThinking()
    {
        // Arrange
        var builder = new ResponsePartsBuilder();

        // Act
        builder.AppendThinking("Step 1. ");
        builder.AppendThinking("Step 2.");

        // Assert
        Assert.Equal("Step 1. Step 2.", builder.AccumulatedThinking);
    }

    #endregion

    #region FlushText Tests

    [Fact]
    public void FlushText_AddsTextPartToParts()
    {
        // Arrange
        var builder = new ResponsePartsBuilder();
        builder.AppendText("Hello World");

        // Act
        builder.FlushText();

        // Assert
        Assert.Single(builder.Parts);
        var textPart = Assert.IsType<TextPart>(builder.Parts[0]);
        Assert.Equal("Hello World", textPart.Text);
    }

    [Fact]
    public void FlushText_ClearsAccumulator()
    {
        // Arrange
        var builder = new ResponsePartsBuilder();
        builder.AppendText("Hello");

        // Act
        builder.FlushText();

        // Assert
        Assert.Equal("", builder.AccumulatedText);
    }

    [Fact]
    public void FlushText_WithNoText_DoesNotAddPart()
    {
        // Arrange
        var builder = new ResponsePartsBuilder();

        // Act
        builder.FlushText();

        // Assert
        Assert.Empty(builder.Parts);
    }

    #endregion

    #region FlushThinking Tests

    [Fact]
    public void FlushThinking_AddsThinkingPartToParts()
    {
        // Arrange
        var builder = new ResponsePartsBuilder();
        builder.AppendThinking("My thoughts");

        // Act
        builder.FlushThinking();

        // Assert
        Assert.Single(builder.Parts);
        var thinkingPart = Assert.IsType<ThinkingPart>(builder.Parts[0]);
        Assert.Equal("My thoughts", thinkingPart.Text);
    }

    [Fact]
    public void FlushThinking_IncludesSignature()
    {
        // Arrange
        var builder = new ResponsePartsBuilder();
        builder.AppendThinking("Thoughts");
        builder.SetThinkingSignature("sig-123");

        // Act
        builder.FlushThinking();

        // Assert
        var thinkingPart = Assert.IsType<ThinkingPart>(builder.Parts[0]);
        Assert.Equal("sig-123", thinkingPart.Signature);
    }

    [Fact]
    public void FlushThinking_ClearsAccumulatorAndSignature()
    {
        // Arrange
        var builder = new ResponsePartsBuilder();
        builder.AppendThinking("Thoughts");
        builder.SetThinkingSignature("sig-123");

        // Act
        builder.FlushThinking();

        // Assert
        Assert.Equal("", builder.AccumulatedThinking);
        // Flushing again should not produce another part
        builder.FlushThinking();
        Assert.Single(builder.Parts);
    }

    [Fact]
    public void FlushThinking_WithNoThinking_DoesNotAddPart()
    {
        // Arrange
        var builder = new ResponsePartsBuilder();

        // Act
        builder.FlushThinking();

        // Assert
        Assert.Empty(builder.Parts);
    }

    #endregion

    #region FlushAll Tests

    [Fact]
    public void FlushAll_FlushesBothTextAndThinking()
    {
        // Arrange
        var builder = new ResponsePartsBuilder();
        builder.AppendThinking("Thoughts");
        builder.AppendText("Text");

        // Act
        builder.FlushAll();

        // Assert
        Assert.Equal(2, builder.Parts.Count);
        Assert.IsType<ThinkingPart>(builder.Parts[0]);
        Assert.IsType<TextPart>(builder.Parts[1]);
    }

    #endregion

    #region Add Tests

    [Fact]
    public void Add_AddsPartDirectly()
    {
        // Arrange
        var builder = new ResponsePartsBuilder();
        var toolPart = new ToolUsePart("id-1", "my_tool", JsonSerializer.Deserialize<JsonElement>("{}"));

        // Act
        builder.Add(toolPart);

        // Assert
        Assert.Single(builder.Parts);
        Assert.Same(toolPart, builder.Parts[0]);
    }

    [Fact]
    public void Add_PreservesOrder()
    {
        // Arrange
        var builder = new ResponsePartsBuilder();

        // Act
        builder.Add(new TextPart("first"));
        builder.Add(new TextPart("second"));
        builder.Add(new TextPart("third"));

        // Assert
        Assert.Equal(3, builder.Parts.Count);
        Assert.Equal("first", ((TextPart)builder.Parts[0]).Text);
        Assert.Equal("second", ((TextPart)builder.Parts[1]).Text);
        Assert.Equal("third", ((TextPart)builder.Parts[2]).Text);
    }

    #endregion

    #region Mixed Usage Tests

    [Fact]
    public void MixedUsage_TextThinkingAndDirectAdds_PreservesCorrectOrder()
    {
        // Arrange
        var builder = new ResponsePartsBuilder();

        // Act
        builder.AppendThinking("thinking...");
        builder.FlushThinking();
        builder.AppendText("some text");
        builder.Add(new ToolUsePart("id-1", "tool", JsonSerializer.Deserialize<JsonElement>("{}")));
        builder.FlushText();

        // Assert
        Assert.Equal(3, builder.Parts.Count);
        Assert.IsType<ThinkingPart>(builder.Parts[0]);
        Assert.IsType<ToolUsePart>(builder.Parts[1]);
        Assert.IsType<TextPart>(builder.Parts[2]);
    }

    #endregion
}
