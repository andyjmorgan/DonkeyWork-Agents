using DonkeyWork.Agents.Actors.Contracts.Models;
using Xunit;

namespace DonkeyWork.Agents.Actors.Tests.Protocol;

public class ConversationMessageTests
{
    #region UserConversationMessage Tests

    [Fact]
    public void UserConversationMessage_PreservesTextAndTimestamp()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow;

        // Act
        var msg = new UserConversationMessage("Hello", Guid.NewGuid(), timestamp);

        // Assert
        Assert.Equal("Hello", msg.Text);
        Assert.Equal(timestamp, msg.Timestamp);
    }

    #endregion

    #region AgentResultConversationMessage Tests

    [Fact]
    public void AgentResultConversationMessage_PreservesAllFields()
    {
        // Arrange
        var result = AgentResult.FromText("output");
        var timestamp = DateTimeOffset.UtcNow;

        // Act
        var msg = new AgentResultConversationMessage("agent-1", "Research", result, false, timestamp);

        // Assert
        Assert.Equal("agent-1", msg.AgentKey);
        Assert.Equal("Research", msg.Label);
        Assert.NotNull(msg.Result);
        Assert.False(msg.IsError);
    }

    [Fact]
    public void AgentResultConversationMessage_WithNullResult_HandlesGracefully()
    {
        // Act
        var msg = new AgentResultConversationMessage("agent-1", "Research", null, true, DateTimeOffset.UtcNow);

        // Assert
        Assert.Null(msg.Result);
        Assert.True(msg.IsError);
    }

    #endregion
}
