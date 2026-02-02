using DonkeyWork.Agents.Agents.Contracts.Models;
using DonkeyWork.Agents.Agents.Core.Services;
using DonkeyWork.Agents.Agents.Core.Tests.Helpers;
using DonkeyWork.Agents.Persistence;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Agents.Core.Tests.Services;

/// <summary>
/// Unit tests for AgentService.
/// Tests CRUD operations and business logic without external dependencies.
/// </summary>
public class AgentServiceTests : IDisposable
{
    private readonly AgentsDbContext _dbContext;
    private readonly AgentService _service;
    private readonly Guid _testUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly TestDataBuilder _builder = new();

    public AgentServiceTests()
    {
        _dbContext = MockDbContext.Create();
        var logger = new Mock<ILogger<AgentService>>();
        _service = new AgentService(_dbContext, logger.Object);
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
    }

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidRequest_CreatesAgentWithDefaultTemplate()
    {
        // Arrange
        var request = new CreateAgentRequestV1
        {
            Name = "test-agent",
            Description = "Test description"
        };

        // Act
        var result = await _service.CreateAsync(request, _testUserId);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(request.Name, result.Name);
        Assert.Equal(request.Description, result.Description);
        Assert.NotEqual(Guid.Empty, result.VersionId);
        Assert.True(result.CreatedAt <= DateTimeOffset.UtcNow);

        // Verify agent was created in database
        var agentInDb = await _dbContext.Agents.FindAsync(result.Id);
        Assert.NotNull(agentInDb);
        Assert.Equal(_testUserId, agentInDb.UserId);

        // Verify initial draft version was created
        var versionInDb = await _dbContext.AgentVersions.FindAsync(result.VersionId);
        Assert.NotNull(versionInDb);
        Assert.Equal(1, versionInDb.VersionNumber);
        Assert.True(versionInDb.IsDraft);
        Assert.NotNull(versionInDb.InputSchema);
        Assert.NotNull(versionInDb.ReactFlowData);
        Assert.NotNull(versionInDb.NodeConfigurations);
    }

    [Fact]
    public async Task CreateAsync_CreatesDefaultStartEndTemplate()
    {
        // Arrange
        var request = TestDataBuilder.CreateAgentRequest();

        // Act
        var result = await _service.CreateAsync(request, _testUserId);

        // Assert - verify default template structure
        var version = await _dbContext.AgentVersions.FindAsync(result.VersionId);
        Assert.NotNull(version);

        // Verify ReactFlowData contains start and end nodes (typed ReactFlowData)
        Assert.NotNull(version.ReactFlowData);
        Assert.Contains(version.ReactFlowData.Nodes, n => n.Data.NodeType == DonkeyWork.Agents.Agents.Contracts.Nodes.Enums.NodeType.Start);
        Assert.Contains(version.ReactFlowData.Nodes, n => n.Data.NodeType == DonkeyWork.Agents.Agents.Contracts.Nodes.Enums.NodeType.End);

        // Verify NodeConfigurations has both nodes (typed Dictionary)
        Assert.Contains(version.NodeConfigurations.Values, c => c.Name == "start");
        Assert.Contains(version.NodeConfigurations.Values, c => c.Name == "end");

        // Verify InputSchema has required structure (typed JsonDocument)
        Assert.NotNull(version.InputSchema);
        Assert.True(version.InputSchema.RootElement.TryGetProperty("type", out var typeProperty));
        Assert.Equal("object", typeProperty.GetString());
    }

    [Fact]
    public async Task CreateAsync_WithNullDescription_CreatesSuccessfully()
    {
        // Arrange
        var request = new CreateAgentRequestV1
        {
            Name = "test-agent",
            Description = null
        };

        // Act
        var result = await _service.CreateAsync(request, _testUserId);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Description);
    }

    [Fact]
    public async Task CreateAsync_MultipleAgents_EachHasUniqueIds()
    {
        // Arrange
        var request1 = TestDataBuilder.CreateAgentRequest("agent-1");
        var request2 = TestDataBuilder.CreateAgentRequest("agent-2");

        // Act
        var result1 = await _service.CreateAsync(request1, _testUserId);
        var result2 = await _service.CreateAsync(request2, _testUserId);

        // Assert
        Assert.NotEqual(result1.Id, result2.Id);
        Assert.NotEqual(result1.VersionId, result2.VersionId);
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithExistingAgent_ReturnsAgent()
    {
        // Arrange
        var agent = _builder.CreateAgentEntity();
        MockDbContext.SeedAgent(_dbContext, agent);

        // Act
        var result = await _service.GetByIdAsync(agent.Id, _testUserId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(agent.Id, result.Id);
        Assert.Equal(agent.Name, result.Name);
        Assert.Equal(agent.Description, result.Description);
        Assert.Null(result.CurrentVersionId); // No published version yet
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentAgent_ReturnsNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _service.GetByIdAsync(nonExistentId, _testUserId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_WithPublishedVersion_ReturnsCurrentVersionId()
    {
        // Arrange
        var agent = _builder.CreateAgentEntity();
        var version = _builder.CreateAgentVersionEntity(agentId: agent.Id, isDraft: false);
        agent.CurrentVersionId = version.Id;
        MockDbContext.SeedAgent(_dbContext, agent, version);

        // Act
        var result = await _service.GetByIdAsync(agent.Id, _testUserId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(version.Id, result.CurrentVersionId);
    }

    #endregion

    #region GetByUserIdAsync Tests

    [Fact]
    public async Task GetByUserIdAsync_WithMultipleAgents_ReturnsAllUserAgents()
    {
        // Arrange
        var agent1 = _builder.CreateAgentEntity(name: "agent-1");
        var agent2 = _builder.CreateAgentEntity(name: "agent-2");
        var agent3 = _builder.CreateAgentEntity(name: "agent-3");
        _dbContext.Agents.AddRange(agent1, agent2, agent3);
        await _dbContext.SaveChangesAsync();

        // Act
        var results = await _service.GetByUserIdAsync(_testUserId);

        // Assert
        Assert.NotNull(results);
        Assert.Equal(3, results.Count);
        Assert.Contains(results, a => a.Id == agent1.Id);
        Assert.Contains(results, a => a.Id == agent2.Id);
        Assert.Contains(results, a => a.Id == agent3.Id);
    }

    [Fact]
    public async Task GetByUserIdAsync_WithNoAgents_ReturnsEmptyList()
    {
        // Act
        var results = await _service.GetByUserIdAsync(_testUserId);

        // Assert
        Assert.NotNull(results);
        Assert.Empty(results);
    }

    [Fact]
    public async Task GetByUserIdAsync_OrdersByCreatedAtDescending()
    {
        // Arrange
        var agent1 = _builder.CreateAgentEntity(name: "oldest");
        await Task.Delay(10); // Ensure different timestamps
        var agent2 = _builder.CreateAgentEntity(name: "middle");
        await Task.Delay(10);
        var agent3 = _builder.CreateAgentEntity(name: "newest");

        _dbContext.Agents.AddRange(agent1, agent2, agent3);
        await _dbContext.SaveChangesAsync();

        // Act
        var results = await _service.GetByUserIdAsync(_testUserId);

        // Assert
        Assert.Equal(3, results.Count);
        Assert.Equal(agent3.Id, results[0].Id); // Newest first
        Assert.Equal(agent2.Id, results[1].Id);
        Assert.Equal(agent1.Id, results[2].Id); // Oldest last
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidRequest_UpdatesAgent()
    {
        // Arrange
        var agent = _builder.CreateAgentEntity();
        MockDbContext.SeedAgent(_dbContext, agent);

        var updateRequest = new CreateAgentRequestV1
        {
            Name = "updated-name",
            Description = "Updated description"
        };

        // Act
        var result = await _service.UpdateAsync(agent.Id, updateRequest, _testUserId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(agent.Id, result.Id);
        Assert.Equal(updateRequest.Name, result.Name);
        Assert.Equal(updateRequest.Description, result.Description);

        // Verify in database
        var updatedAgent = await _dbContext.Agents.FindAsync(agent.Id);
        Assert.NotNull(updatedAgent);
        Assert.Equal(updateRequest.Name, updatedAgent.Name);
        Assert.Equal(updateRequest.Description, updatedAgent.Description);
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistentAgent_ReturnsNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var updateRequest = TestDataBuilder.CreateAgentRequest();

        // Act
        var result = await _service.UpdateAsync(nonExistentId, updateRequest, _testUserId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesTimestamp()
    {
        // Arrange
        var agent = _builder.CreateAgentEntity();
        var originalUpdatedAt = agent.UpdatedAt;
        MockDbContext.SeedAgent(_dbContext, agent);

        await Task.Delay(100); // Ensure timestamp difference

        var updateRequest = TestDataBuilder.CreateAgentRequest("new-name");

        // Act
        var result = await _service.UpdateAsync(agent.Id, updateRequest, _testUserId);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.UpdatedAt);
        Assert.True(result.UpdatedAt > originalUpdatedAt);
    }

    [Fact]
    public async Task UpdateAsync_OnlyUpdatesMetadata_DoesNotAffectVersions()
    {
        // Arrange
        var agent = _builder.CreateAgentEntity();
        var version = _builder.CreateAgentVersionEntity(agentId: agent.Id);
        MockDbContext.SeedAgent(_dbContext, agent, version);

        var updateRequest = TestDataBuilder.CreateAgentRequest("updated-agent");

        // Act
        await _service.UpdateAsync(agent.Id, updateRequest, _testUserId);

        // Assert - version should remain unchanged
        var versionInDb = await _dbContext.AgentVersions.FindAsync(version.Id);
        Assert.NotNull(versionInDb);
        Assert.Equal(version.ReactFlowData, versionInDb.ReactFlowData);
        Assert.Equal(version.NodeConfigurations, versionInDb.NodeConfigurations);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithExistingAgent_DeletesAgent()
    {
        // Arrange
        var agent = _builder.CreateAgentEntity();
        MockDbContext.SeedAgent(_dbContext, agent);

        // Act
        var result = await _service.DeleteAsync(agent.Id, _testUserId);

        // Assert
        Assert.True(result);

        // Verify deleted from database
        var deletedAgent = await _dbContext.Agents.FindAsync(agent.Id);
        Assert.Null(deletedAgent);
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentAgent_ReturnsFalse()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _service.DeleteAsync(nonExistentId, _testUserId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteAsync_CascadesVersionDeletion()
    {
        // Arrange
        var agent = _builder.CreateAgentEntity();
        var version1 = _builder.CreateAgentVersionEntity(agentId: agent.Id, versionNumber: 1, isDraft: false);
        var version2 = _builder.CreateAgentVersionEntity(agentId: agent.Id, versionNumber: 2, isDraft: true);

        _dbContext.Agents.Add(agent);
        _dbContext.AgentVersions.AddRange(version1, version2);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.DeleteAsync(agent.Id, _testUserId);

        // Assert
        Assert.True(result);

        // Verify versions are also deleted (cascade delete via EF configuration)
        var versionsInDb = _dbContext.AgentVersions.Where(v => v.AgentId == agent.Id).ToList();
        Assert.Empty(versionsInDb);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task CreateAsync_WithEmptyName_DoesNotValidateHere()
    {
        // Arrange - validation should happen at controller level
        var request = new CreateAgentRequestV1
        {
            Name = "",
            Description = "Test"
        };

        // Act & Assert - service should accept it (validation is controller's job)
        var result = await _service.CreateAsync(request, _testUserId);
        Assert.NotNull(result);
        Assert.Equal("", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidGuid_ReturnsNull()
    {
        // Arrange
        var invalidId = Guid.Empty;

        // Act
        var result = await _service.GetByIdAsync(invalidId, _testUserId);

        // Assert
        Assert.Null(result);
    }

    #endregion
}
