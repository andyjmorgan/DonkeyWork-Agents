using System.Text.Json;
using DonkeyWork.Agents.Orleans.Contracts.Models;
using Xunit;

namespace DonkeyWork.Agents.Orleans.Tests.Protocol;

public class AgentResultSerializationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        AllowOutOfOrderMetadataProperties = true
    };

    #region AgentResult Tests

    [Fact]
    public void FromText_CreatesResultWithSingleTextPart()
    {
        // Act
        var result = AgentResult.FromText("hello world");

        // Assert
        Assert.Single(result.Parts);
        var textPart = Assert.IsType<AgentTextPart>(result.Parts[0]);
        Assert.Equal("hello world", textPart.Text);
    }

    [Fact]
    public void Empty_CreatesResultWithNoParts()
    {
        // Act
        var result = AgentResult.Empty;

        // Assert
        Assert.Empty(result.Parts);
    }

    [Fact]
    public void AgentResult_WithTextAndCitations_JsonRoundTrips()
    {
        // Arrange
        var result = new AgentResult(
        [
            new AgentTextPart("Some text"),
            new AgentCitationPart("Title", "https://example.com", "cited text")
        ]);

        // Act
        var json = JsonSerializer.Serialize(result, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<AgentResult>(json, JsonOptions);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(2, deserialized.Parts.Count);
        Assert.IsType<AgentTextPart>(deserialized.Parts[0]);
        Assert.IsType<AgentCitationPart>(deserialized.Parts[1]);

        var citation = (AgentCitationPart)deserialized.Parts[1];
        Assert.Equal("Title", citation.Title);
        Assert.Equal("https://example.com", citation.Url);
        Assert.Equal("cited text", citation.CitedText);
    }

    #endregion
}
