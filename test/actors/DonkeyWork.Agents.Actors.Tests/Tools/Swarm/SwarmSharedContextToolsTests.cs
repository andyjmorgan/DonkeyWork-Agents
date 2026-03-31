using System.Text.Json;
using DonkeyWork.Agents.Actors.Contracts.Grains;
using DonkeyWork.Agents.Actors.Contracts.Models;
using DonkeyWork.Agents.Actors.Core;
using DonkeyWork.Agents.Actors.Core.Tools.Swarm;
using DonkeyWork.Agents.Identity.Contracts.Services;
using Moq;
using Xunit;

namespace DonkeyWork.Agents.Actors.Tests.Tools.Swarm;

public class SwarmSharedContextToolsTests
{
    private readonly Mock<IAgentRegistryGrain> _registry = new();
    private readonly Mock<IGrainFactory> _grainFactory = new();
    private readonly Mock<IIdentityContext> _identityContext = new();
    private readonly GrainContext _grainContext;
    private readonly SwarmSharedContextTools _tools = new();
    private readonly Guid _userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly string _conversationId = Guid.NewGuid().ToString();

    public SwarmSharedContextToolsTests()
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
        };
    }

    #region WriteSharedContext Tests

    [Fact]
    public async Task WriteSharedContext_WritesToRegistry()
    {
        // Act
        var result = await _tools.WriteSharedContext("findings", "important data", _grainContext, _identityContext.Object, CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        _registry.Verify(x => x.WriteSharedContextAsync("findings", "important data"), Times.Once);
        var json = JsonSerializer.Deserialize<JsonElement>(result.Content);
        Assert.Equal("written", json.GetProperty("status").GetString());
    }

    #endregion

    #region ReadSharedContext Tests

    [Fact]
    public async Task ReadSharedContext_WithKey_ReturnsValue()
    {
        // Arrange
        _registry.Setup(x => x.ReadSharedContextAsync("findings"))
            .ReturnsAsync("important data");

        // Act
        var result = await _tools.ReadSharedContext("findings", _grainContext, _identityContext.Object);

        // Assert
        Assert.False(result.IsError);
        var json = JsonSerializer.Deserialize<JsonElement>(result.Content);
        Assert.Equal("important data", json.GetProperty("value").GetString());
        Assert.True(json.GetProperty("found").GetBoolean());
    }

    [Fact]
    public async Task ReadSharedContext_WithMissingKey_ReturnsNotFound()
    {
        // Arrange
        _registry.Setup(x => x.ReadSharedContextAsync("missing"))
            .ReturnsAsync((string?)null);

        // Act
        var result = await _tools.ReadSharedContext("missing", _grainContext, _identityContext.Object);

        // Assert
        Assert.False(result.IsError);
        var json = JsonSerializer.Deserialize<JsonElement>(result.Content);
        Assert.False(json.GetProperty("found").GetBoolean());
    }

    [Fact]
    public async Task ReadSharedContext_WithoutKey_ReturnsAll()
    {
        // Arrange
        _registry.Setup(x => x.ReadAllSharedContextAsync())
            .ReturnsAsync(new Dictionary<string, string>
            {
                ["key1"] = "value1",
                ["key2"] = "value2",
            });

        // Act
        var result = await _tools.ReadSharedContext(null, _grainContext, _identityContext.Object);

        // Assert
        Assert.False(result.IsError);
        var json = JsonSerializer.Deserialize<JsonElement>(result.Content);
        Assert.Equal(2, json.GetProperty("count").GetInt32());
    }

    #endregion
}
