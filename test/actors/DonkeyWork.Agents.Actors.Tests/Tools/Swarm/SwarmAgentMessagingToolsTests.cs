using System.Text.Json;
using System.Threading.Channels;
using DonkeyWork.Agents.Actors.Contracts.Grains;
using DonkeyWork.Agents.Actors.Contracts.Models;
using DonkeyWork.Agents.Actors.Core;
using DonkeyWork.Agents.Actors.Core.Tools.Swarm;
using DonkeyWork.Agents.Identity.Contracts.Services;
using Moq;
using Xunit;

namespace DonkeyWork.Agents.Actors.Tests.Tools.Swarm;

public class SwarmAgentMessagingToolsTests
{
    private readonly Mock<IAgentRegistryGrain> _registry = new();
    private readonly Mock<IGrainFactory> _grainFactory = new();
    private readonly Mock<IIdentityContext> _identityContext = new();
    private readonly GrainContext _grainContext;
    private readonly SwarmAgentMessagingTools _tools = new();
    private readonly Guid _userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly string _conversationId = Guid.NewGuid().ToString();

    public SwarmAgentMessagingToolsTests()
    {
        _identityContext.Setup(x => x.UserId).Returns(_userId);

        var registryKey = AgentKeys.Conversation(_userId, Guid.Parse(_conversationId));
        _grainFactory.Setup(x => x.GetGrain<IAgentRegistryGrain>(registryKey, null))
            .Returns(_registry.Object);

        _grainContext = new GrainContext
        {
            GrainKey = $"agent:{_userId}:{_conversationId}:{Guid.NewGuid()}",
            ConversationId = _conversationId,
            UserId = _userId.ToString(),
            GrainFactory = _grainFactory.Object,
            DisplayName = "test-agent",
            MessageInbox = Channel.CreateUnbounded<AgentMessage>(),
        };
    }

    #region SendMessage Tests

    [Fact]
    public async Task SendMessage_WithValidTarget_SendsMessage()
    {
        // Arrange
        var targetKey = "agent:key:123";
        _registry.Setup(x => x.ResolveAgentKeyByNameAsync("researcher"))
            .ReturnsAsync(targetKey);
        _registry.Setup(x => x.SendMessageAsync(
                _grainContext.GrainKey, targetKey, It.IsAny<AgentMessage>()))
            .ReturnsAsync(true);

        // Act
        var result = await _tools.SendMessage("researcher", "hello", _grainContext, _identityContext.Object, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        _registry.Verify(x => x.SendMessageAsync(
            _grainContext.GrainKey, targetKey, It.Is<AgentMessage>(m => m.Content == "hello")), Times.Once);
    }

    [Fact]
    public async Task SendMessage_WithUnknownTarget_ReturnsError()
    {
        // Arrange
        _registry.Setup(x => x.ResolveAgentKeyByNameAsync("unknown"))
            .ReturnsAsync((string?)null);
        _registry.Setup(x => x.ListAsync())
            .ReturnsAsync(new List<TrackedAgent>
            {
                new("key1", "label1", "parent", AgentStatus.Pending, null, DateTime.UtcNow, "researcher"),
            });

        // Act
        var result = await _tools.SendMessage("unknown", "hello", _grainContext, _identityContext.Object, CancellationToken.None);

        // Assert
        Assert.True(result.IsError);
        Assert.Contains("not found", result.Content);
        Assert.Contains("researcher", result.Content);
    }

    [Fact]
    public async Task SendMessage_WithBroadcast_BroadcastsToAll()
    {
        // Act
        var result = await _tools.SendMessage("*", "stop working", _grainContext, _identityContext.Object, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        _registry.Verify(x => x.BroadcastMessageAsync(
            _grainContext.GrainKey, It.Is<AgentMessage>(m => m.Content == "stop working")), Times.Once);
    }

    #endregion

    #region CheckMessages Tests

    [Fact]
    public async Task CheckMessages_WithEmptyInbox_ReturnsEmptyArray()
    {
        // Act
        var result = await _tools.CheckMessages(_grainContext, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        var json = JsonSerializer.Deserialize<JsonElement>(result.Content);
        Assert.Equal(0, json.GetProperty("count").GetInt32());
    }

    [Fact]
    public async Task CheckMessages_WithMessages_ReturnsThem()
    {
        // Arrange
        var msg1 = new AgentMessage("key1", "researcher", "found something", DateTimeOffset.UtcNow);
        var msg2 = new AgentMessage("key2", "delegate", "done", DateTimeOffset.UtcNow);
        _grainContext.MessageInbox!.Writer.TryWrite(msg1);
        _grainContext.MessageInbox!.Writer.TryWrite(msg2);

        // Act
        var result = await _tools.CheckMessages(_grainContext, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        var json = JsonSerializer.Deserialize<JsonElement>(result.Content);
        Assert.Equal(2, json.GetProperty("count").GetInt32());
        var messages = json.GetProperty("messages");
        Assert.Equal("researcher", messages[0].GetProperty("from").GetString());
        Assert.Equal("delegate", messages[1].GetProperty("from").GetString());
    }

    [Fact]
    public async Task CheckMessages_DrainsInbox()
    {
        // Arrange
        _grainContext.MessageInbox!.Writer.TryWrite(
            new AgentMessage("key1", "researcher", "msg", DateTimeOffset.UtcNow));

        // Act - first call drains
        await _tools.CheckMessages(_grainContext, CancellationToken.None);

        // Act - second call should be empty
        var result = await _tools.CheckMessages(_grainContext, CancellationToken.None);

        // Assert
        var json = JsonSerializer.Deserialize<JsonElement>(result.Content);
        Assert.Equal(0, json.GetProperty("count").GetInt32());
    }

    #endregion
}
