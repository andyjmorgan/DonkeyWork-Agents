using System.Net;
using DonkeyWork.Agents.Agents.Contracts.Models;
using DonkeyWork.Agents.Integration.Tests.Base;
using DonkeyWork.Agents.Integration.Tests.Helpers;
using DonkeyWork.Agents.Integration.Tests.Infrastructure.Authentication;
using DonkeyWork.Agents.Integration.Tests.Infrastructure.Containers;

namespace DonkeyWork.Agents.Integration.Tests.Tests.Controllers;

[Trait("Category", "Integration")]
public class AgentVersionsControllerTests : ControllerIntegrationTestBase
{
    private const string AgentsBaseUrl = "/api/v1/agents";

    public AgentVersionsControllerTests(InfrastructureFixture infrastructure)
        : base(infrastructure)
    {
    }

    private string VersionsUrl(Guid agentId) => $"{AgentsBaseUrl}/{agentId}/versions";

    #region SaveDraft Tests

    [Fact]
    public async Task SaveDraft_WithNewAgent_CreatesDraftVersion()
    {
        // Arrange - Create agent
        var agent = await PostAsync<CreateAgentResponseV1>(AgentsBaseUrl, TestDataBuilder.CreateAgentRequest());
        var versionRequest = TestDataBuilder.CreateSaveVersionRequest();

        // Act
        var version = await PostAsync<GetAgentVersionResponseV1>(VersionsUrl(agent!.Id), versionRequest);

        // Assert
        Assert.NotNull(version);
        Assert.Equal(1, version.VersionNumber);
        Assert.True(version.IsDraft);
        Assert.Equal(agent.Id, version.AgentId);
    }

    [Fact]
    public async Task SaveDraft_MultipleTimes_UpdatesSameVersion()
    {
        // Arrange
        var agent = await PostAsync<CreateAgentResponseV1>(AgentsBaseUrl, TestDataBuilder.CreateAgentRequest());

        // Act - Save draft twice
        var version1 = await PostAsync<GetAgentVersionResponseV1>(
            VersionsUrl(agent!.Id),
            TestDataBuilder.CreateSaveVersionRequest(inputSchema: """{"type": "object", "properties": {"v1": {}}}"""));

        var version2 = await PostAsync<GetAgentVersionResponseV1>(
            VersionsUrl(agent.Id),
            TestDataBuilder.CreateSaveVersionRequest(inputSchema: """{"type": "object", "properties": {"v2": {}}}"""));

        // Assert - Same version ID and version number
        Assert.NotNull(version1);
        Assert.NotNull(version2);
        Assert.Equal(version1.Id, version2.Id);
        Assert.Equal(1, version2.VersionNumber);
        Assert.True(version2.IsDraft);
    }

    [Fact]
    public async Task SaveDraft_WithNonExistentAgent_ReturnsNotFound()
    {
        // Arrange
        var nonExistentAgentId = Guid.NewGuid();
        var versionRequest = TestDataBuilder.CreateSaveVersionRequest();

        // Act
        var response = await PostResponseAsync(VersionsUrl(nonExistentAgentId), versionRequest);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SaveDraft_PreservesReactFlowDataStructure()
    {
        // Arrange
        var agent = await PostAsync<CreateAgentResponseV1>(AgentsBaseUrl, TestDataBuilder.CreateAgentRequest());
        var reactFlowData = """
        {
            "nodes": [
                { "id": "node-1", "type": "start", "position": { "x": 100, "y": 200 }, "data": { "label": "Start" } },
                { "id": "node-2", "type": "end", "position": { "x": 500, "y": 200 }, "data": { "label": "End" } }
            ],
            "edges": [
                { "id": "edge-1", "source": "node-1", "target": "node-2" }
            ],
            "viewport": { "x": 50, "y": 100, "zoom": 1.5 }
        }
        """;
        // Node configurations must match the node IDs in reactFlowData
        var nodeConfigurations = """
        {
            "node-1": { "name": "start-node", "inputSchema": { "type": "object" } },
            "node-2": { "name": "end-node" }
        }
        """;
        var versionRequest = TestDataBuilder.CreateSaveVersionRequest(reactFlowData: reactFlowData, nodeConfigurations: nodeConfigurations);

        // Act
        var version = await PostAsync<GetAgentVersionResponseV1>(VersionsUrl(agent!.Id), versionRequest);

        // Assert
        Assert.NotNull(version);
        var reactFlowJson = version.ReactFlowData.GetRawText();
        Assert.Contains("node-1", reactFlowJson);
        Assert.Contains("node-2", reactFlowJson);
        Assert.Contains("edge-1", reactFlowJson);
        Assert.Contains("1.5", reactFlowJson); // zoom
    }

    [Fact]
    public async Task SaveDraft_PreservesNodeConfigurations()
    {
        // Arrange
        var agent = await PostAsync<CreateAgentResponseV1>(AgentsBaseUrl, TestDataBuilder.CreateAgentRequest());
        // Custom reactFlowData and nodeConfigurations must match
        var reactFlowData = """
        {
            "nodes": [
                { "id": "node-1", "type": "start", "position": { "x": 100, "y": 100 }, "data": {} },
                { "id": "node-2", "type": "end", "position": { "x": 400, "y": 100 }, "data": {} }
            ],
            "edges": [
                { "id": "e1", "source": "node-1", "target": "node-2" }
            ],
            "viewport": { "x": 0, "y": 0, "zoom": 1 }
        }
        """;
        var nodeConfigurations = """
        {
            "node-1": { "name": "start-node", "inputSchema": { "type": "object" }, "customSetting": "value" },
            "node-2": { "name": "end-node", "enabled": true }
        }
        """;
        var versionRequest = TestDataBuilder.CreateSaveVersionRequest(reactFlowData: reactFlowData, nodeConfigurations: nodeConfigurations);

        // Act
        var version = await PostAsync<GetAgentVersionResponseV1>(VersionsUrl(agent!.Id), versionRequest);

        // Assert
        Assert.NotNull(version);
        var nodeConfigJson = version.NodeConfigurations.GetRawText();
        Assert.Contains("node-1", nodeConfigJson);
        Assert.Contains("customSetting", nodeConfigJson);
    }

    [Fact]
    public async Task SaveDraft_PreservesInputSchema()
    {
        // Arrange
        var agent = await PostAsync<CreateAgentResponseV1>(AgentsBaseUrl, TestDataBuilder.CreateAgentRequest());
        var inputSchema = """{"type": "object", "properties": {"name": {"type": "string"}, "age": {"type": "number"}}}""";
        var versionRequest = TestDataBuilder.CreateSaveVersionRequest(inputSchema: inputSchema);

        // Act
        var version = await PostAsync<GetAgentVersionResponseV1>(VersionsUrl(agent!.Id), versionRequest);

        // Assert
        Assert.NotNull(version);
        var inputSchemaJson = version.InputSchema.GetRawText();
        Assert.Contains("name", inputSchemaJson);
        Assert.Contains("age", inputSchemaJson);
    }

    #endregion

    #region Publish Tests

    [Fact]
    public async Task Publish_WithDraftVersion_PublishesSuccessfully()
    {
        // Arrange - Create agent and save draft
        var agent = await PostAsync<CreateAgentResponseV1>(AgentsBaseUrl, TestDataBuilder.CreateAgentRequest());
        await PostAsync<GetAgentVersionResponseV1>(VersionsUrl(agent!.Id), TestDataBuilder.CreateSaveVersionRequest());

        // Act
        var published = await PostAsync<GetAgentVersionResponseV1>($"{VersionsUrl(agent.Id)}/publish", new { });

        // Assert
        Assert.NotNull(published);
        Assert.False(published.IsDraft);
        Assert.Equal(1, published.VersionNumber);
        Assert.NotNull(published.PublishedAt);
    }

    [Fact]
    public async Task Publish_WithoutDraft_ReturnsNotFound()
    {
        // Arrange - Create agent (which has initial draft), publish it, then try to publish again
        var agent = await PostAsync<CreateAgentResponseV1>(AgentsBaseUrl, TestDataBuilder.CreateAgentRequest());
        await PostAsync<GetAgentVersionResponseV1>($"{VersionsUrl(agent!.Id)}/publish", new { }); // Publish initial draft

        // Act - Try to publish when there's no draft
        var response = await PostResponseAsync($"{VersionsUrl(agent.Id)}/publish", new { });

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Publish_WithNonExistentAgent_ReturnsNotFound()
    {
        // Arrange
        var nonExistentAgentId = Guid.NewGuid();

        // Act
        var response = await PostResponseAsync($"{VersionsUrl(nonExistentAgentId)}/publish", new { });

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PublishWorkflow_SavePublishSave_CreatesNewDraftVersion()
    {
        // Arrange - Create agent
        var agent = await PostAsync<CreateAgentResponseV1>(AgentsBaseUrl, TestDataBuilder.CreateAgentRequest());

        // Act - Save v1, publish, save v2
        await PostAsync<GetAgentVersionResponseV1>(VersionsUrl(agent!.Id), TestDataBuilder.CreateSaveVersionRequest());
        var publishedV1 = await PostAsync<GetAgentVersionResponseV1>($"{VersionsUrl(agent.Id)}/publish", new { });
        var draftV2 = await PostAsync<GetAgentVersionResponseV1>(VersionsUrl(agent.Id), TestDataBuilder.CreateSaveVersionRequest());

        // Assert
        Assert.NotNull(publishedV1);
        Assert.NotNull(draftV2);
        Assert.Equal(1, publishedV1.VersionNumber);
        Assert.False(publishedV1.IsDraft);
        Assert.Equal(2, draftV2.VersionNumber);
        Assert.True(draftV2.IsDraft);
    }

    #endregion

    #region GetVersion Tests

    [Fact]
    public async Task GetVersion_WithExistingVersion_ReturnsVersion()
    {
        // Arrange
        var agent = await PostAsync<CreateAgentResponseV1>(AgentsBaseUrl, TestDataBuilder.CreateAgentRequest());
        var saved = await PostAsync<GetAgentVersionResponseV1>(VersionsUrl(agent!.Id), TestDataBuilder.CreateSaveVersionRequest());

        // Act
        var version = await GetAsync<GetAgentVersionResponseV1>($"{VersionsUrl(agent.Id)}/{saved!.Id}");

        // Assert
        Assert.NotNull(version);
        Assert.Equal(saved.Id, version.Id);
        Assert.Equal(saved.VersionNumber, version.VersionNumber);
    }

    [Fact]
    public async Task GetVersion_WithNonExistentVersion_ReturnsNotFound()
    {
        // Arrange
        var agent = await PostAsync<CreateAgentResponseV1>(AgentsBaseUrl, TestDataBuilder.CreateAgentRequest());
        var nonExistentVersionId = Guid.NewGuid();

        // Act
        var response = await GetResponseAsync($"{VersionsUrl(agent!.Id)}/{nonExistentVersionId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region ListVersions Tests

    [Fact]
    public async Task ListVersions_WithMultipleVersions_ReturnsAllVersionsDescending()
    {
        // Arrange - Create agent, publish v1, save v2
        var agent = await PostAsync<CreateAgentResponseV1>(AgentsBaseUrl, TestDataBuilder.CreateAgentRequest());
        await PostAsync<GetAgentVersionResponseV1>(VersionsUrl(agent!.Id), TestDataBuilder.CreateSaveVersionRequest());
        await PostAsync<GetAgentVersionResponseV1>($"{VersionsUrl(agent.Id)}/publish", new { });
        await PostAsync<GetAgentVersionResponseV1>(VersionsUrl(agent.Id), TestDataBuilder.CreateSaveVersionRequest());

        // Act
        var versions = await GetAsync<List<GetAgentVersionResponseV1>>(VersionsUrl(agent.Id));

        // Assert
        Assert.NotNull(versions);
        Assert.Equal(2, versions.Count);
        Assert.Equal(2, versions[0].VersionNumber); // Latest first
        Assert.Equal(1, versions[1].VersionNumber);
    }

    [Fact]
    public async Task ListVersions_WithNewAgent_ReturnsInitialDraftVersion()
    {
        // Arrange - Create agent (which includes an initial draft version)
        var agent = await PostAsync<CreateAgentResponseV1>(AgentsBaseUrl, TestDataBuilder.CreateAgentRequest());

        // Act
        var versions = await GetAsync<List<GetAgentVersionResponseV1>>(VersionsUrl(agent!.Id));

        // Assert - New agents start with a draft version
        Assert.NotNull(versions);
        Assert.Single(versions);
        Assert.True(versions[0].IsDraft);
        Assert.Equal(1, versions[0].VersionNumber);
    }

    #endregion

    #region Timestamp Tests

    [Fact]
    public async Task VersionTimestamps_AreCorrect()
    {
        // Arrange
        var agent = await PostAsync<CreateAgentResponseV1>(AgentsBaseUrl, TestDataBuilder.CreateAgentRequest());

        // Act - Save draft
        var beforeSave = DateTimeOffset.UtcNow;
        var draft = await PostAsync<GetAgentVersionResponseV1>(VersionsUrl(agent!.Id), TestDataBuilder.CreateSaveVersionRequest());
        var afterSave = DateTimeOffset.UtcNow;

        // Assert - CreatedAt set, PublishedAt null
        Assert.NotNull(draft);
        Assert.True(draft.CreatedAt >= beforeSave.AddSeconds(-1));
        Assert.True(draft.CreatedAt <= afterSave.AddSeconds(1));
        Assert.Null(draft.PublishedAt);

        // Act - Publish
        var beforePublish = DateTimeOffset.UtcNow;
        var published = await PostAsync<GetAgentVersionResponseV1>($"{VersionsUrl(agent.Id)}/publish", new { });
        var afterPublish = DateTimeOffset.UtcNow;

        // Assert - PublishedAt set
        Assert.NotNull(published);
        Assert.NotNull(published.PublishedAt);
        Assert.True(published.PublishedAt >= beforePublish.AddSeconds(-1));
        Assert.True(published.PublishedAt <= afterPublish.AddSeconds(1));
    }

    #endregion

    #region User Isolation Tests

    [Fact]
    public async Task SaveDraft_AgentBelongingToAnotherUser_ReturnsNotFound()
    {
        // Arrange - Create agent as user 1
        var user1 = TestUser.CreateRandom();
        SetTestUser(user1);
        var agent = await PostAsync<CreateAgentResponseV1>(AgentsBaseUrl, TestDataBuilder.CreateAgentRequest());

        // Act - Try to save draft as user 2
        var user2 = TestUser.CreateRandom();
        SetTestUser(user2);
        var response = await PostResponseAsync(VersionsUrl(agent!.Id), TestDataBuilder.CreateSaveVersionRequest());

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ListVersions_AgentBelongingToAnotherUser_ReturnsEmptyOrNotFound()
    {
        // Arrange - Create agent and version as user 1
        var user1 = TestUser.CreateRandom();
        SetTestUser(user1);
        var agent = await PostAsync<CreateAgentResponseV1>(AgentsBaseUrl, TestDataBuilder.CreateAgentRequest());
        await PostAsync<GetAgentVersionResponseV1>(VersionsUrl(agent!.Id), TestDataBuilder.CreateSaveVersionRequest());

        // Act - Try to list as user 2
        var user2 = TestUser.CreateRandom();
        SetTestUser(user2);
        var versions = await GetAsync<List<GetAgentVersionResponseV1>>(VersionsUrl(agent.Id));

        // Assert - Should return empty list (agent not found for user 2)
        Assert.NotNull(versions);
        Assert.Empty(versions);
    }

    #endregion
}
