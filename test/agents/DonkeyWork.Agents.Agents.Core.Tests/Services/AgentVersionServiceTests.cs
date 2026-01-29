using DonkeyWork.Agents.Agents.Contracts.Models;
using DonkeyWork.Agents.Agents.Core.Services;
using DonkeyWork.Agents.Agents.Core.Tests.Helpers;
using DonkeyWork.Agents.Persistence;
using DonkeyWork.Agents.Persistence.Entities.Agents;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Agents.Core.Tests.Services;

/// <summary>
/// Unit tests for AgentVersionService.
/// Tests draft/publish workflow, version management, and credential mappings.
/// </summary>
public class AgentVersionServiceTests : IDisposable
{
    private readonly AgentsDbContext _dbContext;
    private readonly AgentVersionService _service;
    private readonly Guid _testUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly TestDataBuilder _builder = new();

    public AgentVersionServiceTests()
    {
        _dbContext = MockDbContext.Create();
        var logger = new Mock<ILogger<AgentVersionService>>();
        _service = new AgentVersionService(_dbContext, logger.Object);
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
    }

    #region SaveDraftAsync Tests

    [Fact]
    public async Task SaveDraftAsync_WithNewAgent_CreatesNewDraft()
    {
        // Arrange
        var agent = _builder.CreateAgentEntity();
        _dbContext.Agents.Add(agent);
        await _dbContext.SaveChangesAsync();

        var request = TestDataBuilder.CreateSaveVersionRequest();

        // Act
        var result = await _service.SaveDraftAsync(agent.Id, request, _testUserId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(agent.Id, result.AgentId);
        Assert.Equal(1, result.VersionNumber);
        Assert.True(result.IsDraft);
        Assert.Null(result.PublishedAt);

        // Verify in database
        var versionInDb = await _dbContext.AgentVersions.FindAsync(result.Id);
        Assert.NotNull(versionInDb);
        Assert.True(versionInDb.IsDraft);
    }

    [Fact]
    public async Task SaveDraftAsync_WithExistingDraft_UpdatesExistingDraft()
    {
        // Arrange
        var agent = _builder.CreateAgentEntity();
        var existingDraft = _builder.CreateAgentVersionEntity(agentId: agent.Id, versionNumber: 1, isDraft: true);
        _dbContext.Agents.Add(agent);
        _dbContext.AgentVersions.Add(existingDraft);
        await _dbContext.SaveChangesAsync();

        var request = TestDataBuilder.CreateSaveVersionRequest();

        // Act
        var result = await _service.SaveDraftAsync(agent.Id, request, _testUserId);

        // Assert
        Assert.Equal(existingDraft.Id, result.Id);
        Assert.Equal(1, result.VersionNumber);
        Assert.True(result.IsDraft);

        // Verify only one version exists
        var versionsCount = _dbContext.AgentVersions.Count(v => v.AgentId == agent.Id);
        Assert.Equal(1, versionsCount);
    }

    [Fact]
    public async Task SaveDraftAsync_AfterPublish_CreatesNewDraftWithIncrementedVersion()
    {
        // Arrange
        var agent = _builder.CreateAgentEntity();
        var publishedVersion = _builder.CreateAgentVersionEntity(agentId: agent.Id, versionNumber: 1, isDraft: false);
        _dbContext.Agents.Add(agent);
        _dbContext.AgentVersions.Add(publishedVersion);
        await _dbContext.SaveChangesAsync();

        var request = TestDataBuilder.CreateSaveVersionRequest();

        // Act
        var result = await _service.SaveDraftAsync(agent.Id, request, _testUserId);

        // Assert
        Assert.NotEqual(publishedVersion.Id, result.Id);
        Assert.Equal(2, result.VersionNumber);
        Assert.True(result.IsDraft);

        // Verify both versions exist
        var versionsCount = _dbContext.AgentVersions.Count(v => v.AgentId == agent.Id);
        Assert.Equal(2, versionsCount);
    }

    [Fact]
    public async Task SaveDraftAsync_WithNonExistentAgent_ThrowsException()
    {
        // Arrange
        var nonExistentAgentId = Guid.NewGuid();
        var request = TestDataBuilder.CreateSaveVersionRequest();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _service.SaveDraftAsync(nonExistentAgentId, request, _testUserId)
        );
    }

    [Fact]
    public async Task SaveDraftAsync_WithCredentialMappings_SavesMappings()
    {
        // Arrange
        var agent = _builder.CreateAgentEntity();
        _dbContext.Agents.Add(agent);
        await _dbContext.SaveChangesAsync();

        var credentialId = Guid.NewGuid();
        var credentials = new List<CredentialMappingV1>
        {
            TestDataBuilder.CreateCredentialMapping("model-1", credentialId)
        };
        var request = TestDataBuilder.CreateSaveVersionRequestWithCredentials(credentials);

        // Act
        var result = await _service.SaveDraftAsync(agent.Id, request, _testUserId);

        // Assert
        var mappings = _dbContext.AgentVersionCredentialMappings
            .Where(m => m.AgentVersionId == result.Id)
            .ToList();

        Assert.Single(mappings);
        Assert.Equal("model-1", mappings[0].NodeId);
        Assert.Equal(credentialId, mappings[0].CredentialId);
    }

    [Fact]
    public async Task SaveDraftAsync_UpdatesCredentialMappings_RemovesOldAndAddsNew()
    {
        // Arrange
        var agent = _builder.CreateAgentEntity();
        var draft = _builder.CreateAgentVersionEntity(agentId: agent.Id, isDraft: true);
        var oldMapping = new AgentVersionCredentialMappingEntity
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            AgentVersionId = draft.Id,
            NodeId = "old-node",
            CredentialId = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.Agents.Add(agent);
        _dbContext.AgentVersions.Add(draft);
        _dbContext.AgentVersionCredentialMappings.Add(oldMapping);
        await _dbContext.SaveChangesAsync();

        var newCredentialId = Guid.NewGuid();
        var newCredentials = new List<CredentialMappingV1>
        {
            TestDataBuilder.CreateCredentialMapping("model-1", newCredentialId)
        };
        var request = TestDataBuilder.CreateSaveVersionRequestWithCredentials(newCredentials);

        // Act
        var result = await _service.SaveDraftAsync(agent.Id, request, _testUserId);

        // Assert
        var mappings = _dbContext.AgentVersionCredentialMappings
            .Where(m => m.AgentVersionId == result.Id)
            .ToList();

        Assert.Single(mappings);
        Assert.Equal("model-1", mappings[0].NodeId);
        Assert.Equal(newCredentialId, mappings[0].CredentialId);
        Assert.NotEqual(oldMapping.Id, mappings[0].Id);
    }

    [Fact]
    public async Task SaveDraftAsync_PreservesReactFlowData()
    {
        // Arrange
        var agent = _builder.CreateAgentEntity();
        _dbContext.Agents.Add(agent);
        await _dbContext.SaveChangesAsync();

        var request = TestDataBuilder.CreateSaveVersionRequest();

        // Act
        var result = await _service.SaveDraftAsync(agent.Id, request, _testUserId);

        // Assert
        Assert.True(result.ReactFlowData.TryGetProperty("nodes", out var nodes));
        Assert.True(result.ReactFlowData.TryGetProperty("edges", out var edges));
        Assert.True(result.ReactFlowData.TryGetProperty("viewport", out var viewport));
    }

    [Fact]
    public async Task SaveDraftAsync_PreservesNodeConfigurations()
    {
        // Arrange
        var agent = _builder.CreateAgentEntity();
        _dbContext.Agents.Add(agent);
        await _dbContext.SaveChangesAsync();

        var request = TestDataBuilder.CreateSaveVersionRequest();

        // Act
        var result = await _service.SaveDraftAsync(agent.Id, request, _testUserId);

        // Assert
        Assert.Equal(System.Text.Json.JsonValueKind.Object, result.NodeConfigurations.ValueKind);
    }

    [Fact]
    public async Task SaveDraftAsync_EnrichesNodeConfigurationsWithTypeDiscriminator()
    {
        // Arrange
        var agent = _builder.CreateAgentEntity();
        _dbContext.Agents.Add(agent);
        await _dbContext.SaveChangesAsync();

        var request = TestDataBuilder.CreateSaveVersionRequest();

        // Act
        var result = await _service.SaveDraftAsync(agent.Id, request, _testUserId);

        // Assert - Verify stored JSON includes type discriminator for each node
        var versionInDb = await _dbContext.AgentVersions.FindAsync(result.Id);
        Assert.NotNull(versionInDb);

        var nodeConfigs = System.Text.Json.JsonDocument.Parse(versionInDb.NodeConfigurations);
        foreach (var nodeConfig in nodeConfigs.RootElement.EnumerateObject())
        {
            Assert.True(nodeConfig.Value.TryGetProperty("type", out var typeProperty),
                $"Node configuration for '{nodeConfig.Name}' should have type discriminator");
            Assert.NotNull(typeProperty.GetString());
        }
    }

    [Fact]
    public async Task SaveDraftAsync_PreservesInputSchema()
    {
        // Arrange
        var agent = _builder.CreateAgentEntity();
        _dbContext.Agents.Add(agent);
        await _dbContext.SaveChangesAsync();

        var request = TestDataBuilder.CreateSaveVersionRequest();

        // Act
        var result = await _service.SaveDraftAsync(agent.Id, request, _testUserId);

        // Assert
        Assert.True(result.InputSchema.TryGetProperty("type", out var typeProperty));
        Assert.Equal("object", typeProperty.GetString());
    }

    [Fact]
    public async Task SaveDraftAsync_UpdatesTimestamp()
    {
        // Arrange
        var agent = _builder.CreateAgentEntity();
        var draft = _builder.CreateAgentVersionEntity(agentId: agent.Id, isDraft: true);
        var originalUpdatedAt = draft.UpdatedAt;

        _dbContext.Agents.Add(agent);
        _dbContext.AgentVersions.Add(draft);
        await _dbContext.SaveChangesAsync();

        await Task.Delay(100); // Ensure timestamp difference

        var request = TestDataBuilder.CreateSaveVersionRequest();

        // Act
        var result = await _service.SaveDraftAsync(agent.Id, request, _testUserId);

        // Assert
        var updatedDraft = await _dbContext.AgentVersions.FindAsync(result.Id);
        Assert.NotNull(updatedDraft);
        Assert.True(updatedDraft.UpdatedAt > originalUpdatedAt);
    }

    #endregion

    #region PublishAsync Tests

    [Fact]
    public async Task PublishAsync_WithDraft_PublishesDraft()
    {
        // Arrange
        var agent = _builder.CreateAgentEntity();
        var draft = _builder.CreateAgentVersionEntity(agentId: agent.Id, versionNumber: 1, isDraft: true);
        _dbContext.Agents.Add(agent);
        _dbContext.AgentVersions.Add(draft);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.PublishAsync(agent.Id, _testUserId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(draft.Id, result.Id);
        Assert.Equal(1, result.VersionNumber);
        Assert.False(result.IsDraft);
        Assert.NotNull(result.PublishedAt);
    }

    [Fact]
    public async Task PublishAsync_UpdatesAgentCurrentVersionId()
    {
        // Arrange
        var agent = _builder.CreateAgentEntity();
        var draft = _builder.CreateAgentVersionEntity(agentId: agent.Id, versionNumber: 1, isDraft: true);
        _dbContext.Agents.Add(agent);
        _dbContext.AgentVersions.Add(draft);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.PublishAsync(agent.Id, _testUserId);

        // Assert
        var updatedAgent = await _dbContext.Agents.FindAsync(agent.Id);
        Assert.NotNull(updatedAgent);
        Assert.Equal(result.Id, updatedAgent.CurrentVersionId);
    }

    [Fact]
    public async Task PublishAsync_WithoutDraft_ThrowsException()
    {
        // Arrange
        var agent = _builder.CreateAgentEntity();
        _dbContext.Agents.Add(agent);
        await _dbContext.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _service.PublishAsync(agent.Id, _testUserId)
        );
    }

    [Fact]
    public async Task PublishAsync_WithNonExistentAgent_ThrowsException()
    {
        // Arrange
        var nonExistentAgentId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _service.PublishAsync(nonExistentAgentId, _testUserId)
        );
    }

    [Fact]
    public async Task PublishAsync_UpdatesTimestamps()
    {
        // Arrange
        var agent = _builder.CreateAgentEntity();
        var draft = _builder.CreateAgentVersionEntity(agentId: agent.Id, isDraft: true);
        _dbContext.Agents.Add(agent);
        _dbContext.AgentVersions.Add(draft);
        await _dbContext.SaveChangesAsync();

        var beforePublish = DateTimeOffset.UtcNow;
        await Task.Delay(100);

        // Act
        var result = await _service.PublishAsync(agent.Id, _testUserId);

        // Assert
        Assert.NotNull(result.PublishedAt);
        Assert.True(result.PublishedAt >= beforePublish);
    }

    [Fact]
    public async Task PublishAsync_WithPreviousPublishedVersion_UpdatesCurrentVersionId()
    {
        // Arrange
        var agent = _builder.CreateAgentEntity();
        var publishedV1 = _builder.CreateAgentVersionEntity(agentId: agent.Id, versionNumber: 1, isDraft: false);
        var draftV2 = _builder.CreateAgentVersionEntity(agentId: agent.Id, versionNumber: 2, isDraft: true);
        agent.CurrentVersionId = publishedV1.Id;

        _dbContext.Agents.Add(agent);
        _dbContext.AgentVersions.AddRange(publishedV1, draftV2);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.PublishAsync(agent.Id, _testUserId);

        // Assert
        Assert.Equal(draftV2.Id, result.Id);
        Assert.Equal(2, result.VersionNumber);

        var updatedAgent = await _dbContext.Agents.FindAsync(agent.Id);
        Assert.NotNull(updatedAgent);
        Assert.Equal(draftV2.Id, updatedAgent.CurrentVersionId);
    }

    #endregion

    #region GetVersionAsync Tests

    [Fact]
    public async Task GetVersionAsync_WithExistingVersion_ReturnsVersion()
    {
        // Arrange
        var agent = _builder.CreateAgentEntity();
        var version = _builder.CreateAgentVersionEntity(agentId: agent.Id);
        _dbContext.Agents.Add(agent);
        _dbContext.AgentVersions.Add(version);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetVersionAsync(agent.Id, version.Id, _testUserId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(version.Id, result.Id);
        Assert.Equal(agent.Id, result.AgentId);
    }

    [Fact]
    public async Task GetVersionAsync_WithNonExistentVersion_ReturnsNull()
    {
        // Arrange
        var agent = _builder.CreateAgentEntity();
        _dbContext.Agents.Add(agent);
        await _dbContext.SaveChangesAsync();

        var nonExistentVersionId = Guid.NewGuid();

        // Act
        var result = await _service.GetVersionAsync(agent.Id, nonExistentVersionId, _testUserId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetVersionAsync_WithWrongAgentId_ReturnsNull()
    {
        // Arrange
        var agent1 = _builder.CreateAgentEntity(name: "agent-1");
        var agent2 = _builder.CreateAgentEntity(name: "agent-2");
        var version = _builder.CreateAgentVersionEntity(agentId: agent1.Id);

        _dbContext.Agents.AddRange(agent1, agent2);
        _dbContext.AgentVersions.Add(version);
        await _dbContext.SaveChangesAsync();

        // Act - try to get version from wrong agent
        var result = await _service.GetVersionAsync(agent2.Id, version.Id, _testUserId);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetVersionsAsync Tests

    [Fact]
    public async Task GetVersionsAsync_WithMultipleVersions_ReturnsAllVersionsDescending()
    {
        // Arrange
        var agent = _builder.CreateAgentEntity();
        var version1 = _builder.CreateAgentVersionEntity(agentId: agent.Id, versionNumber: 1, isDraft: false);
        var version2 = _builder.CreateAgentVersionEntity(agentId: agent.Id, versionNumber: 2, isDraft: false);
        var version3 = _builder.CreateAgentVersionEntity(agentId: agent.Id, versionNumber: 3, isDraft: true);

        _dbContext.Agents.Add(agent);
        _dbContext.AgentVersions.AddRange(version1, version2, version3);
        await _dbContext.SaveChangesAsync();

        // Act
        var results = await _service.GetVersionsAsync(agent.Id, _testUserId);

        // Assert
        Assert.Equal(3, results.Count);
        Assert.Equal(3, results[0].VersionNumber); // Newest first
        Assert.Equal(2, results[1].VersionNumber);
        Assert.Equal(1, results[2].VersionNumber); // Oldest last
    }

    [Fact]
    public async Task GetVersionsAsync_WithNoVersions_ReturnsEmptyList()
    {
        // Arrange
        var agent = _builder.CreateAgentEntity();
        _dbContext.Agents.Add(agent);
        await _dbContext.SaveChangesAsync();

        // Act
        var results = await _service.GetVersionsAsync(agent.Id, _testUserId);

        // Assert
        Assert.NotNull(results);
        Assert.Empty(results);
    }

    [Fact]
    public async Task GetVersionsAsync_OnlyReturnsVersionsForSpecificAgent()
    {
        // Arrange
        var agent1 = _builder.CreateAgentEntity(name: "agent-1");
        var agent2 = _builder.CreateAgentEntity(name: "agent-2");
        var version1 = _builder.CreateAgentVersionEntity(agentId: agent1.Id);
        var version2 = _builder.CreateAgentVersionEntity(agentId: agent2.Id);

        _dbContext.Agents.AddRange(agent1, agent2);
        _dbContext.AgentVersions.AddRange(version1, version2);
        await _dbContext.SaveChangesAsync();

        // Act
        var results = await _service.GetVersionsAsync(agent1.Id, _testUserId);

        // Assert
        Assert.Single(results);
        Assert.Equal(agent1.Id, results[0].AgentId);
    }

    [Fact]
    public async Task GetVersionsAsync_IncludesBothDraftAndPublished()
    {
        // Arrange
        var agent = _builder.CreateAgentEntity();
        var published = _builder.CreateAgentVersionEntity(agentId: agent.Id, versionNumber: 1, isDraft: false);
        var draft = _builder.CreateAgentVersionEntity(agentId: agent.Id, versionNumber: 2, isDraft: true);

        _dbContext.Agents.Add(agent);
        _dbContext.AgentVersions.AddRange(published, draft);
        await _dbContext.SaveChangesAsync();

        // Act
        var results = await _service.GetVersionsAsync(agent.Id, _testUserId);

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Contains(results, v => v.IsDraft);
        Assert.Contains(results, v => !v.IsDraft);
    }

    #endregion

    #region Version Workflow Tests

    [Fact]
    public async Task VersionWorkflow_SavePublishSave_CreatesCorrectVersionSequence()
    {
        // Arrange
        var agent = _builder.CreateAgentEntity();
        _dbContext.Agents.Add(agent);
        await _dbContext.SaveChangesAsync();

        // Act 1: Save initial draft (v1)
        var draft1 = await _service.SaveDraftAsync(agent.Id, TestDataBuilder.CreateSaveVersionRequest(), _testUserId);
        Assert.Equal(1, draft1.VersionNumber);
        Assert.True(draft1.IsDraft);

        // Act 2: Publish v1
        var published1 = await _service.PublishAsync(agent.Id, _testUserId);
        Assert.Equal(1, published1.VersionNumber);
        Assert.False(published1.IsDraft);

        // Act 3: Save new draft (v2)
        var draft2 = await _service.SaveDraftAsync(agent.Id, TestDataBuilder.CreateSaveVersionRequest(), _testUserId);
        Assert.Equal(2, draft2.VersionNumber);
        Assert.True(draft2.IsDraft);

        // Act 4: Publish v2
        var published2 = await _service.PublishAsync(agent.Id, _testUserId);
        Assert.Equal(2, published2.VersionNumber);
        Assert.False(published2.IsDraft);

        // Assert: Verify all versions in database
        var allVersions = await _service.GetVersionsAsync(agent.Id, _testUserId);
        Assert.Equal(2, allVersions.Count);
        Assert.All(allVersions, v => Assert.False(v.IsDraft));
    }

    [Fact]
    public async Task VersionWorkflow_MultipleSavesBeforePublish_UpdatesSameDraft()
    {
        // Arrange
        var agent = _builder.CreateAgentEntity();
        _dbContext.Agents.Add(agent);
        await _dbContext.SaveChangesAsync();

        // Act: Save draft three times
        var draft1 = await _service.SaveDraftAsync(agent.Id, TestDataBuilder.CreateSaveVersionRequest(), _testUserId);
        var draft2 = await _service.SaveDraftAsync(agent.Id, TestDataBuilder.CreateSaveVersionRequest(), _testUserId);
        var draft3 = await _service.SaveDraftAsync(agent.Id, TestDataBuilder.CreateSaveVersionRequest(), _testUserId);

        // Assert: All three saves update the same draft
        Assert.Equal(draft1.Id, draft2.Id);
        Assert.Equal(draft2.Id, draft3.Id);
        Assert.Equal(1, draft1.VersionNumber);

        // Verify only one version exists
        var allVersions = await _service.GetVersionsAsync(agent.Id, _testUserId);
        Assert.Single(allVersions);
    }

    #endregion

    #region Action Node Tests

    [Fact]
    public async Task SaveDraftAsync_WithValidActionNode_SavesActionConfiguration()
    {
        // Arrange
        var agent = _builder.CreateAgentEntity();
        _dbContext.Agents.Add(agent);
        await _dbContext.SaveChangesAsync();

        var request = TestDataBuilder.CreateSaveVersionRequestWithActionNode("http_request");

        // Act
        var result = await _service.SaveDraftAsync(agent.Id, request, _testUserId);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsDraft);

        // Verify action node configuration was stored with type discriminator
        var versionInDb = await _dbContext.AgentVersions.FindAsync(result.Id);
        Assert.NotNull(versionInDb);

        var nodeConfigs = System.Text.Json.JsonDocument.Parse(versionInDb.NodeConfigurations);
        Assert.True(nodeConfigs.RootElement.TryGetProperty("action-1", out var actionConfig));
        Assert.True(actionConfig.TryGetProperty("type", out var typeProperty));
        Assert.Equal("action", typeProperty.GetString());
        Assert.True(actionConfig.TryGetProperty("actionType", out var actionTypeProperty));
        Assert.Equal("http_request", actionTypeProperty.GetString());
    }

    [Fact]
    public async Task SaveDraftAsync_WithMissingActionType_ThrowsException()
    {
        // Arrange
        var agent = _builder.CreateAgentEntity();
        _dbContext.Agents.Add(agent);
        await _dbContext.SaveChangesAsync();

        var request = TestDataBuilder.CreateSaveVersionRequestWithInvalidActionNode();

        // Act & Assert
        // JsonException is thrown during deserialization because actionType is a required property
        var exception = await Assert.ThrowsAsync<System.Text.Json.JsonException>(
            async () => await _service.SaveDraftAsync(agent.Id, request, _testUserId)
        );

        Assert.Contains("actionType", exception.Message);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task SaveDraftAsync_WithEmptyCredentialMappings_RemovesAllExistingMappings()
    {
        // Arrange
        var agent = _builder.CreateAgentEntity();
        var draft = _builder.CreateAgentVersionEntity(agentId: agent.Id, isDraft: true);
        var mapping = new AgentVersionCredentialMappingEntity
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            AgentVersionId = draft.Id,
            NodeId = "model-1",
            CredentialId = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.Agents.Add(agent);
        _dbContext.AgentVersions.Add(draft);
        _dbContext.AgentVersionCredentialMappings.Add(mapping);
        await _dbContext.SaveChangesAsync();

        var request = TestDataBuilder.CreateSaveVersionRequest(); // No credentials

        // Act
        await _service.SaveDraftAsync(agent.Id, request, _testUserId);

        // Assert
        var mappings = _dbContext.AgentVersionCredentialMappings
            .Where(m => m.AgentVersionId == draft.Id)
            .ToList();
        Assert.Empty(mappings);
    }

    [Fact]
    public async Task SaveDraftAsync_WithNullOutputSchema_SavesNull()
    {
        // Arrange
        var agent = _builder.CreateAgentEntity();
        _dbContext.Agents.Add(agent);
        await _dbContext.SaveChangesAsync();

        var request = TestDataBuilder.CreateSaveVersionRequest(); // OutputSchema is null

        // Act
        var result = await _service.SaveDraftAsync(agent.Id, request, _testUserId);

        // Assert
        Assert.Null(result.OutputSchema);
    }

    #endregion
}
