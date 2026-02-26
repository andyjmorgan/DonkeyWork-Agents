using System.Text.Json;
using DonkeyWork.Agents.Orleans.Contracts.Messages;
using Xunit;

namespace DonkeyWork.Agents.Orleans.Tests.Protocol;

public class InternalMessageSerializationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        AllowOutOfOrderMetadataProperties = true
    };

    #region InternalMessage Polymorphic Serialization Tests

    [Fact]
    public void InternalContentMessage_JsonRoundTrips_WithDiscriminator()
    {
        // Arrange
        InternalMessage message = new InternalContentMessage
        {
            Role = InternalMessageRole.User,
            Content = "Hello"
        };

        // Act
        var json = JsonSerializer.Serialize(message, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<InternalMessage>(json, JsonOptions);

        // Assert
        Assert.Contains("$type", json);
        var content = Assert.IsType<InternalContentMessage>(deserialized);
        Assert.Equal("Hello", content.Content);
        Assert.Equal(InternalMessageRole.User, content.Role);
    }

    [Fact]
    public void InternalToolResultMessage_JsonRoundTrips_WithDiscriminator()
    {
        // Arrange
        InternalMessage message = new InternalToolResultMessage
        {
            Role = InternalMessageRole.User,
            ToolUseId = "tool-123",
            Content = "result data",
            IsError = true
        };

        // Act
        var json = JsonSerializer.Serialize(message, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<InternalMessage>(json, JsonOptions);

        // Assert
        var toolResult = Assert.IsType<InternalToolResultMessage>(deserialized);
        Assert.Equal("tool-123", toolResult.ToolUseId);
        Assert.Equal("result data", toolResult.Content);
        Assert.True(toolResult.IsError);
    }

    [Fact]
    public void InternalAssistantMessage_JsonRoundTrips_WithDiscriminator()
    {
        // Arrange
        InternalMessage message = new InternalAssistantMessage
        {
            Role = InternalMessageRole.Assistant,
            TextContent = "response text",
            ToolUses = [],
            ContentBlocks = [new InternalTextBlock("some text")]
        };

        // Act
        var json = JsonSerializer.Serialize(message, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<InternalMessage>(json, JsonOptions);

        // Assert
        var assistant = Assert.IsType<InternalAssistantMessage>(deserialized);
        Assert.Equal("response text", assistant.TextContent);
        Assert.Single(assistant.ContentBlocks);
        Assert.IsType<InternalTextBlock>(assistant.ContentBlocks[0]);
    }

    #endregion

    #region InternalContentBlock Serialization Tests

    [Fact]
    public void ContentBlocks_AllTypes_JsonRoundTrip()
    {
        // Arrange
        var blocks = new List<InternalContentBlock>
        {
            new InternalTextBlock("hello"),
            new InternalThinkingBlock("thinking...", "sig-abc"),
            new InternalCitationBlock("Title", "https://url.com", "cited"),
        };

        // Act
        var json = JsonSerializer.Serialize(blocks, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<List<InternalContentBlock>>(json, JsonOptions);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(3, deserialized.Count);
        Assert.IsType<InternalTextBlock>(deserialized[0]);
        Assert.IsType<InternalThinkingBlock>(deserialized[1]);
        Assert.IsType<InternalCitationBlock>(deserialized[2]);

        var thinking = (InternalThinkingBlock)deserialized[1];
        Assert.Equal("thinking...", thinking.Text);
        Assert.Equal("sig-abc", thinking.Signature);
    }

    #endregion
}
