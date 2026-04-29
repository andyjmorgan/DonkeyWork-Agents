using DonkeyWork.Agents.Actors.Api.Endpoints;
using DonkeyWork.Agents.Actors.Api.EventBus;
using DonkeyWork.Agents.Actors.Contracts.Grains;
using DonkeyWork.Agents.Actors.Contracts.Models;
using DonkeyWork.Agents.Common.MessageBus.Transport;
using Microsoft.Extensions.Logging;
using Moq;
using NATS.Client.JetStream;
using NATS.Client.ObjectStore;
using Xunit;

namespace DonkeyWork.Agents.Actors.Tests.Api;

public class ConversationRpcTargetTests
{
    private static AgentEventConsumerFactory CreateConsumerFactory()
    {
        var js = new Mock<INatsJSContext>();
        var obj = new Mock<INatsObjContext>();
        var serializer = new MessagePackPayloadSerializer();
        var registry = new PayloadTypeRegistry();
        var log = new Mock<ILogger<StreamConsumer>>();
        return new AgentEventConsumerFactory(js.Object, obj.Object, serializer, registry, log.Object);
    }

    #region Message Tests

    [Fact]
    public async Task Message_PostsMessageWithoutResubscribing()
    {
        // Arrange
        var grain = new Mock<IConversationGrain>();
        var factory = CreateConsumerFactory();
        var target = new ConversationRpcTarget(grain.Object, "11111111-1111-1111-1111-111111111111", "22222222-2222-2222-2222-222222222222", "conv:11111111-1111-1111-1111-111111111111:22222222-2222-2222-2222-222222222222", factory);

        // Act
        await target.Message("Hello");

        // Assert
        grain.Verify(g => g.SubscribeAsync(It.IsAny<IAgentResponseObserver>()), Times.Never);
        grain.Verify(g => g.PostUserMessageAsync("Hello", It.IsAny<Guid>()), Times.Once);
    }

    [Fact]
    public async Task Message_ReturnsQueuedStatus()
    {
        // Arrange
        var grain = new Mock<IConversationGrain>();
        var factory = CreateConsumerFactory();
        var target = new ConversationRpcTarget(grain.Object, "11111111-1111-1111-1111-111111111111", "22222222-2222-2222-2222-222222222222", "conv:11111111-1111-1111-1111-111111111111:22222222-2222-2222-2222-222222222222", factory);

        // Act
        var result = await target.Message("Hello");

        // Assert
        var status = result.GetType().GetProperty("status")?.GetValue(result)?.ToString();
        var turnId = result.GetType().GetProperty("turnId")?.GetValue(result);
        Assert.Equal("queued", status);
        Assert.NotNull(turnId);
        Assert.IsType<Guid>(turnId);
    }

    #endregion

    #region Cancel Tests

    [Fact]
    public async Task Cancel_WithKnownPrefix_PassesKeyThrough()
    {
        // Arrange
        var grain = new Mock<IConversationGrain>();
        var factory = CreateConsumerFactory();
        var target = new ConversationRpcTarget(grain.Object, "11111111-1111-1111-1111-111111111111", "22222222-2222-2222-2222-222222222222", "conv:11111111-1111-1111-1111-111111111111:22222222-2222-2222-2222-222222222222", factory);

        // Act
        await target.Cancel("agent:11111111-1111-1111-1111-111111111111:22222222-2222-2222-2222-222222222222:33333333-3333-3333-3333-333333333333", "active");

        // Assert — known prefix passes through unchanged
        grain.Verify(g => g.CancelByKeyAsync("agent:11111111-1111-1111-1111-111111111111:22222222-2222-2222-2222-222222222222:33333333-3333-3333-3333-333333333333", "active"), Times.Once);
    }

    [Fact]
    public async Task Cancel_WithUnknownPrefix_ResolvesToGrainKey()
    {
        // Arrange
        var grain = new Mock<IConversationGrain>();
        var factory = CreateConsumerFactory();
        var target = new ConversationRpcTarget(grain.Object, "11111111-1111-1111-1111-111111111111", "22222222-2222-2222-2222-222222222222", "conv:11111111-1111-1111-1111-111111111111:22222222-2222-2222-2222-222222222222", factory);

        // Act — frontend sends "swarm:{conversationId}" which has no known prefix
        await target.Cancel("swarm:22222222-2222-2222-2222-222222222222", "active");

        // Assert — resolved to the actual grain key for self-cancel
        grain.Verify(g => g.CancelByKeyAsync("conv:11111111-1111-1111-1111-111111111111:22222222-2222-2222-2222-222222222222", "active"), Times.Once);
    }

    [Fact]
    public async Task Cancel_WithNullScope_PassesNull()
    {
        // Arrange
        var grain = new Mock<IConversationGrain>();
        var factory = CreateConsumerFactory();
        var target = new ConversationRpcTarget(grain.Object, "11111111-1111-1111-1111-111111111111", "22222222-2222-2222-2222-222222222222", "conv:11111111-1111-1111-1111-111111111111:22222222-2222-2222-2222-222222222222", factory);

        // Act
        await target.Cancel("swarm:some-id");

        // Assert
        grain.Verify(g => g.CancelByKeyAsync("conv:11111111-1111-1111-1111-111111111111:22222222-2222-2222-2222-222222222222", null), Times.Once);
    }

    #endregion

    #region ListAgents Tests

    [Fact]
    public async Task ListAgents_ReturnsGrainResult()
    {
        // Arrange
        var grain = new Mock<IConversationGrain>();
        var factory = CreateConsumerFactory();
        var expected = new List<TrackedAgent>
        {
            new("agent-1", "researcher", "parent-1", AgentStatus.Completed, null, DateTime.UtcNow),
            new("agent-2", "writer", "parent-1", AgentStatus.Pending, null, DateTime.UtcNow),
        };
        grain.Setup(g => g.ListAgentsAsync()).ReturnsAsync(expected);
        var target = new ConversationRpcTarget(grain.Object, "11111111-1111-1111-1111-111111111111", "22222222-2222-2222-2222-222222222222", "conv:11111111-1111-1111-1111-111111111111:22222222-2222-2222-2222-222222222222", factory);

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
        var factory = CreateConsumerFactory();
        grain.Setup(g => g.ListAgentsAsync()).ReturnsAsync(new List<TrackedAgent>());
        var target = new ConversationRpcTarget(grain.Object, "11111111-1111-1111-1111-111111111111", "22222222-2222-2222-2222-222222222222", "conv:11111111-1111-1111-1111-111111111111:22222222-2222-2222-2222-222222222222", factory);

        // Act
        var result = await target.ListAgents();

        // Assert
        Assert.Empty(result);
    }

    #endregion
}
