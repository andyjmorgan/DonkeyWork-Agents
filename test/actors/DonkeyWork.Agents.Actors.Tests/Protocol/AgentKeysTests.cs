using DonkeyWork.Agents.Actors.Contracts.Models;
using Xunit;

namespace DonkeyWork.Agents.Actors.Tests.Protocol;

public class AgentKeysTests
{
    private static readonly Guid TestUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TestConversationId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid TestTaskId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    #region Conversation Tests

    [Fact]
    public void Conversation_WithUserIdAndConversationId_ReturnsCorrectFormat()
    {
        // Act
        var key = AgentKeys.Conversation(TestUserId, TestConversationId);

        // Assert
        Assert.Equal("conv:11111111-1111-1111-1111-111111111111:22222222-2222-2222-2222-222222222222", key);
    }

    [Fact]
    public void Conversation_IncludesPrefix()
    {
        // Act
        var key = AgentKeys.Conversation(TestUserId, TestConversationId);

        // Assert
        Assert.StartsWith(AgentKeys.ConversationPrefix, key);
    }

    #endregion

    #region Create Tests

    [Fact]
    public void Create_WithDelegatePrefix_IncludesAllComponents()
    {
        // Act
        var key = AgentKeys.Create(AgentKeys.DelegatePrefix, TestUserId, TestConversationId, TestTaskId);

        // Assert
        Assert.StartsWith("delegate:", key);
        Assert.Contains(TestUserId.ToString(), key);
        Assert.Contains(TestConversationId.ToString(), key);
        Assert.Contains(TestTaskId.ToString(), key);
    }

    [Fact]
    public void Create_WithAgentPrefix_IncludesAllComponents()
    {
        // Act
        var key = AgentKeys.Create(AgentKeys.AgentPrefix, TestUserId, TestConversationId, TestTaskId);

        // Assert
        Assert.StartsWith("agent:", key);
        Assert.Contains(TestUserId.ToString(), key);
        Assert.Contains(TestConversationId.ToString(), key);
        Assert.Contains(TestTaskId.ToString(), key);
    }

    #endregion
}
