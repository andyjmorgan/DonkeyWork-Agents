using DonkeyWork.Agents.Orleans.Api.Endpoints;
using DonkeyWork.Agents.Orleans.Contracts.Grains;
using DonkeyWork.Agents.Orleans.Contracts.Models;
using Moq;
using Xunit;

namespace DonkeyWork.Agents.Orleans.Tests.Api;

public class ConversationRpcTargetTests
{
    #region Message Tests

    [Fact]
    public async Task Message_SubscribesAndPostsMessage()
    {
        // Arrange
        var grain = new Mock<IConversationGrain>();
        var observer = new Mock<IAgentResponseObserver>();
        var target = new ConversationRpcTarget(grain.Object, observer.Object);

        // Act
        var result = await target.Message("Hello");

        // Assert
        grain.Verify(g => g.SubscribeAsync(observer.Object), Times.Once);
        grain.Verify(g => g.PostUserMessageAsync("Hello"), Times.Once);
    }

    [Fact]
    public async Task Message_ReturnsQueuedStatus()
    {
        // Arrange
        var grain = new Mock<IConversationGrain>();
        var observer = new Mock<IAgentResponseObserver>();
        var target = new ConversationRpcTarget(grain.Object, observer.Object);

        // Act
        var result = await target.Message("Hello");

        // Assert
        var status = result.GetType().GetProperty("status")?.GetValue(result)?.ToString();
        Assert.Equal("queued", status);
    }

    [Fact]
    public async Task Message_ResubscribesOnEachCall()
    {
        // Arrange
        var grain = new Mock<IConversationGrain>();
        var observer = new Mock<IAgentResponseObserver>();
        var target = new ConversationRpcTarget(grain.Object, observer.Object);

        // Act
        await target.Message("First");
        await target.Message("Second");

        // Assert
        grain.Verify(g => g.SubscribeAsync(observer.Object), Times.Exactly(2));
    }

    #endregion

    #region Cancel Tests

    [Fact]
    public async Task Cancel_DelegatesToGrain()
    {
        // Arrange
        var grain = new Mock<IConversationGrain>();
        var observer = new Mock<IAgentResponseObserver>();
        var target = new ConversationRpcTarget(grain.Object, observer.Object);

        // Act
        await target.Cancel("agent-1", "active");

        // Assert
        grain.Verify(g => g.CancelByKeyAsync("agent-1", "active"), Times.Once);
    }

    [Fact]
    public async Task Cancel_WithNullScope_PassesNull()
    {
        // Arrange
        var grain = new Mock<IConversationGrain>();
        var observer = new Mock<IAgentResponseObserver>();
        var target = new ConversationRpcTarget(grain.Object, observer.Object);

        // Act
        await target.Cancel("agent-1");

        // Assert
        grain.Verify(g => g.CancelByKeyAsync("agent-1", null), Times.Once);
    }

    #endregion

    #region ListAgents Tests

    [Fact]
    public async Task ListAgents_ReturnsGrainResult()
    {
        // Arrange
        var grain = new Mock<IConversationGrain>();
        var observer = new Mock<IAgentResponseObserver>();
        var expected = new List<TrackedAgent>
        {
            new("agent-1", "researcher", "parent-1", AgentStatus.Completed, null, DateTime.UtcNow),
            new("agent-2", "writer", "parent-1", AgentStatus.Pending, null, DateTime.UtcNow),
        };
        grain.Setup(g => g.ListAgentsAsync()).ReturnsAsync(expected);
        var target = new ConversationRpcTarget(grain.Object, observer.Object);

        // Act
        var result = await target.ListAgents();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("agent-1", result[0].AgentKey);
        Assert.Equal("agent-2", result[1].AgentKey);
    }

    [Fact]
    public async Task ListAgents_WithEmpty_ReturnsEmpty()
    {
        // Arrange
        var grain = new Mock<IConversationGrain>();
        var observer = new Mock<IAgentResponseObserver>();
        grain.Setup(g => g.ListAgentsAsync()).ReturnsAsync(new List<TrackedAgent>());
        var target = new ConversationRpcTarget(grain.Object, observer.Object);

        // Act
        var result = await target.ListAgents();

        // Assert
        Assert.Empty(result);
    }

    #endregion
}
