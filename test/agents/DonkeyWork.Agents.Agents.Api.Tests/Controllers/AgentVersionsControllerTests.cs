using System.Net;
using System.Net.Http.Json;
using DonkeyWork.Agents.Agents.Contracts.Models;
using DonkeyWork.Agents.Agents.Api.Tests.Helpers;
using Xunit;

namespace DonkeyWork.Agents.Agents.Api.Tests.Controllers;

/// <summary>
/// Integration tests for AgentVersionsController.
/// Tests draft/publish workflow and version management.
/// </summary>
[Collection(nameof(AgentsApiCollection))]
public class AgentVersionsControllerTests
{
    private readonly HttpClient _client;
    private readonly AgentsApiFactory _factory;

    public AgentVersionsControllerTests(AgentsApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<Guid> CreateTestAgentAsync()
    {
        var request = TestDataBuilder.CreateAgentRequest();
        var response = await _client.PostAsJsonAsync("/api/v1/agents", request);
        var agent = await response.Content.ReadFromJsonAsync<CreateAgentResponseV1>();
        Assert.NotNull(agent);
        return agent.Id;
    }

    [Fact]
    public async Task SaveDraft_WithNewAgent_UpdatesExistingDraft()
    {
        // Arrange
        var agentId = await CreateTestAgentAsync();
        var saveRequest = TestDataBuilder.CreateSaveVersionRequest();

        // Act
        var response = await _client.PostAsJsonAsync($"/api/v1/agents/{agentId}/versions", saveRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var version = await response.Content.ReadFromJsonAsync<GetAgentVersionResponseV1>();
        Assert.NotNull(version);
        Assert.Equal(agentId, version.AgentId);
        Assert.Equal(1, version.VersionNumber);
        Assert.True(version.IsDraft);
        Assert.Null(version.PublishedAt);
    }

    [Fact]
    public async Task SaveDraft_MultipleTimes_UpdatesSameVersion()
    {
        // Arrange
        var agentId = await CreateTestAgentAsync();
        var saveRequest1 = TestDataBuilder.CreateSaveVersionRequest();
        var saveRequest2 = TestDataBuilder.CreateSaveVersionRequest();

        // Act - save twice
        var response1 = await _client.PostAsJsonAsync($"/api/v1/agents/{agentId}/versions", saveRequest1);
        var version1 = await response1.Content.ReadFromJsonAsync<GetAgentVersionResponseV1>();

        var response2 = await _client.PostAsJsonAsync($"/api/v1/agents/{agentId}/versions", saveRequest2);
        var version2 = await response2.Content.ReadFromJsonAsync<GetAgentVersionResponseV1>();

        // Assert - same version ID, same version number
        Assert.NotNull(version1);
        Assert.NotNull(version2);
        Assert.Equal(version1.Id, version2.Id);
        Assert.Equal(1, version1.VersionNumber);
        Assert.Equal(1, version2.VersionNumber);
    }

    [Fact]
    public async Task SaveDraft_WithNonExistentAgent_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var saveRequest = TestDataBuilder.CreateSaveVersionRequest();

        // Act
        var response = await _client.PostAsJsonAsync($"/api/v1/agents/{nonExistentId}/versions", saveRequest);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Publish_WithDraftVersion_PublishesAndCreatesNewDraft()
    {
        // Arrange
        var agentId = await CreateTestAgentAsync();

        // Act - publish the initial draft
        var publishResponse = await _client.PostAsync($"/api/v1/agents/{agentId}/versions/publish", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, publishResponse.StatusCode);

        var publishedVersion = await publishResponse.Content.ReadFromJsonAsync<GetAgentVersionResponseV1>();
        Assert.NotNull(publishedVersion);
        Assert.Equal(1, publishedVersion.VersionNumber);
        Assert.False(publishedVersion.IsDraft);
        Assert.NotNull(publishedVersion.PublishedAt);

        // Verify agent's CurrentVersionId is updated
        var agentResponse = await _client.GetAsync($"/api/v1/agents/{agentId}");
        var agent = await agentResponse.Content.ReadFromJsonAsync<GetAgentResponseV1>();
        Assert.NotNull(agent);
        Assert.Equal(publishedVersion.Id, agent.CurrentVersionId);
    }

    [Fact]
    public async Task Publish_WithoutDraft_ReturnsNotFound()
    {
        // Arrange - create agent and publish, then try to publish again without new draft
        var agentId = await CreateTestAgentAsync();
        await _client.PostAsync($"/api/v1/agents/{agentId}/versions/publish", null);

        // Act - try to publish again (no draft exists)
        var response = await _client.PostAsync($"/api/v1/agents/{agentId}/versions/publish", null);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Publish_WithNonExistentAgent_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.PostAsync($"/api/v1/agents/{nonExistentId}/versions/publish", null);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PublishWorkflow_SavePublishSave_CreatesNewDraftVersion()
    {
        // Arrange
        var agentId = await CreateTestAgentAsync();

        // Act 1 - Publish initial draft (v1)
        var publish1Response = await _client.PostAsync($"/api/v1/agents/{agentId}/versions/publish", null);
        var published1 = await publish1Response.Content.ReadFromJsonAsync<GetAgentVersionResponseV1>();
        Assert.NotNull(published1);
        Assert.Equal(1, published1.VersionNumber);

        // Act 2 - Save new draft
        var saveRequest = TestDataBuilder.CreateSaveVersionRequest();
        var saveResponse = await _client.PostAsJsonAsync($"/api/v1/agents/{agentId}/versions", saveRequest);
        var draft2 = await saveResponse.Content.ReadFromJsonAsync<GetAgentVersionResponseV1>();
        Assert.NotNull(draft2);

        // Assert - new draft has version number 2
        Assert.Equal(2, draft2.VersionNumber);
        Assert.True(draft2.IsDraft);

        // Act 3 - Publish the new draft
        var publish2Response = await _client.PostAsync($"/api/v1/agents/{agentId}/versions/publish", null);
        var published2 = await publish2Response.Content.ReadFromJsonAsync<GetAgentVersionResponseV1>();
        Assert.NotNull(published2);

        // Assert - published version is v2
        Assert.Equal(2, published2.VersionNumber);
        Assert.False(published2.IsDraft);
        Assert.NotNull(published2.PublishedAt);
    }

    [Fact]
    public async Task GetVersion_WithExistingVersion_ReturnsVersion()
    {
        // Arrange
        var agentId = await CreateTestAgentAsync();

        // Get the initial draft version ID
        var versionsResponse = await _client.GetAsync($"/api/v1/agents/{agentId}/versions");
        var versions = await versionsResponse.Content.ReadFromJsonAsync<List<GetAgentVersionResponseV1>>();
        Assert.NotNull(versions);
        Assert.NotEmpty(versions);
        var versionId = versions[0].Id;

        // Act
        var response = await _client.GetAsync($"/api/v1/agents/{agentId}/versions/{versionId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var version = await response.Content.ReadFromJsonAsync<GetAgentVersionResponseV1>();
        Assert.NotNull(version);
        Assert.Equal(versionId, version.Id);
        Assert.Equal(agentId, version.AgentId);
    }

    [Fact]
    public async Task GetVersion_WithNonExistentVersion_ReturnsNotFound()
    {
        // Arrange
        var agentId = await CreateTestAgentAsync();
        var nonExistentVersionId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/v1/agents/{agentId}/versions/{nonExistentVersionId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ListVersions_WithMultipleVersions_ReturnsAllVersionsDescending()
    {
        // Arrange
        var agentId = await CreateTestAgentAsync();

        // Publish v1
        await _client.PostAsync($"/api/v1/agents/{agentId}/versions/publish", null);

        // Create and publish v2
        await _client.PostAsJsonAsync($"/api/v1/agents/{agentId}/versions", TestDataBuilder.CreateSaveVersionRequest());
        await _client.PostAsync($"/api/v1/agents/{agentId}/versions/publish", null);

        // Create draft v3
        await _client.PostAsJsonAsync($"/api/v1/agents/{agentId}/versions", TestDataBuilder.CreateSaveVersionRequest());

        // Act
        var response = await _client.GetAsync($"/api/v1/agents/{agentId}/versions");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var versions = await response.Content.ReadFromJsonAsync<List<GetAgentVersionResponseV1>>();
        Assert.NotNull(versions);
        Assert.Equal(3, versions.Count);

        // Verify descending order
        Assert.Equal(3, versions[0].VersionNumber);
        Assert.True(versions[0].IsDraft);

        Assert.Equal(2, versions[1].VersionNumber);
        Assert.False(versions[1].IsDraft);

        Assert.Equal(1, versions[2].VersionNumber);
        Assert.False(versions[2].IsDraft);
    }

    [Fact]
    public async Task ListVersions_WithNewAgent_ReturnsInitialDraft()
    {
        // Arrange
        var agentId = await CreateTestAgentAsync();

        // Act
        var response = await _client.GetAsync($"/api/v1/agents/{agentId}/versions");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var versions = await response.Content.ReadFromJsonAsync<List<GetAgentVersionResponseV1>>();
        Assert.NotNull(versions);
        Assert.Single(versions);
        Assert.Equal(1, versions[0].VersionNumber);
        Assert.True(versions[0].IsDraft);
    }

    [Fact]
    public async Task SaveDraft_PreservesReactFlowDataStructure()
    {
        // Arrange
        var agentId = await CreateTestAgentAsync();
        var saveRequest = TestDataBuilder.CreateSaveVersionRequest();

        // Act
        var response = await _client.PostAsJsonAsync($"/api/v1/agents/{agentId}/versions", saveRequest);

        // Assert
        var version = await response.Content.ReadFromJsonAsync<GetAgentVersionResponseV1>();
        Assert.NotNull(version);

        // Verify ReactFlowData structure is preserved
        var reactFlowData = version.ReactFlowData;
        Assert.True(reactFlowData.TryGetProperty("nodes", out var nodes));
        Assert.True(reactFlowData.TryGetProperty("edges", out var edges));
        Assert.True(reactFlowData.TryGetProperty("viewport", out var viewport));

        Assert.Equal(System.Text.Json.JsonValueKind.Array, nodes.ValueKind);
        Assert.Equal(System.Text.Json.JsonValueKind.Array, edges.ValueKind);
        Assert.Equal(System.Text.Json.JsonValueKind.Object, viewport.ValueKind);
    }

    [Fact]
    public async Task SaveDraft_PreservesNodeConfigurations()
    {
        // Arrange
        var agentId = await CreateTestAgentAsync();
        var saveRequest = TestDataBuilder.CreateSaveVersionRequest();

        // Act
        var response = await _client.PostAsJsonAsync($"/api/v1/agents/{agentId}/versions", saveRequest);

        // Assert
        var version = await response.Content.ReadFromJsonAsync<GetAgentVersionResponseV1>();
        Assert.NotNull(version);

        var nodeConfigs = version.NodeConfigurations;
        Assert.Equal(System.Text.Json.JsonValueKind.Object, nodeConfigs.ValueKind);

        // Should have at least start and end node configurations
        var properties = nodeConfigs.EnumerateObject().ToList();
        Assert.True(properties.Count >= 2);
    }

    [Fact]
    public async Task SaveDraft_PreservesInputSchema()
    {
        // Arrange
        var agentId = await CreateTestAgentAsync();
        var saveRequest = TestDataBuilder.CreateSaveVersionRequest();

        // Act
        var response = await _client.PostAsJsonAsync($"/api/v1/agents/{agentId}/versions", saveRequest);

        // Assert
        var version = await response.Content.ReadFromJsonAsync<GetAgentVersionResponseV1>();
        Assert.NotNull(version);

        var inputSchema = version.InputSchema;
        Assert.True(inputSchema.TryGetProperty("type", out var typeProperty));
        Assert.Equal("object", typeProperty.GetString());

        Assert.True(inputSchema.TryGetProperty("properties", out var properties));
        Assert.Equal(System.Text.Json.JsonValueKind.Object, properties.ValueKind);
    }

    [Fact]
    public async Task VersionTimestamps_AreCorrect()
    {
        // Arrange - capture timestamp before creating agent
        var beforeCreate = DateTimeOffset.UtcNow;
        var agentId = await CreateTestAgentAsync();
        var afterCreate = DateTimeOffset.UtcNow;

        // Act - Publish
        await Task.Delay(100); // Small delay to ensure timestamp difference
        var beforePublish = DateTimeOffset.UtcNow;
        var publishResponse = await _client.PostAsync($"/api/v1/agents/{agentId}/versions/publish", null);
        var afterPublish = DateTimeOffset.UtcNow;

        // Assert
        var version = await publishResponse.Content.ReadFromJsonAsync<GetAgentVersionResponseV1>();
        Assert.NotNull(version);
        Assert.True(version.CreatedAt >= beforeCreate);
        Assert.True(version.CreatedAt <= afterCreate);
        Assert.NotNull(version.PublishedAt);
        Assert.True(version.PublishedAt >= beforePublish);
        Assert.True(version.PublishedAt <= afterPublish);
        Assert.True(version.PublishedAt >= version.CreatedAt);
    }
}
