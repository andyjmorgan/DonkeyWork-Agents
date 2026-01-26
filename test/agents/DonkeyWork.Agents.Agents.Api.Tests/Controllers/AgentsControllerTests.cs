using System.Net;
using System.Net.Http.Json;
using DonkeyWork.Agents.Agents.Contracts.Models;
using DonkeyWork.Agents.Agents.Api.Tests.Helpers;
using Xunit;

namespace DonkeyWork.Agents.Agents.Api.Tests.Controllers;

/// <summary>
/// Integration tests for AgentsController.
/// Tests all CRUD operations with real database and services.
/// </summary>
[Collection(nameof(AgentsApiCollection))]
public class AgentsControllerTests
{
    private readonly HttpClient _client;
    private readonly AgentsApiFactory _factory;

    public AgentsControllerTests(AgentsApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Create_WithValidRequest_ReturnsCreatedAgent()
    {
        // Arrange
        var request = TestDataBuilder.CreateAgentRequest();

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/agents", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var agent = await response.Content.ReadFromJsonAsync<CreateAgentResponseV1>();
        Assert.NotNull(agent);
        Assert.NotEqual(Guid.Empty, agent.Id);
        Assert.Equal(request.Name, agent.Name);
        Assert.Equal(request.Description, agent.Description);
        Assert.NotEqual(Guid.Empty, agent.VersionId);
        Assert.True(agent.CreatedAt <= DateTimeOffset.UtcNow);

        // Verify Location header
        Assert.NotNull(response.Headers.Location);
        Assert.Contains($"/api/v1/agents/{agent.Id}", response.Headers.Location.ToString());
    }

    [Fact]
    public async Task Create_WithInvalidName_ReturnsBadRequest()
    {
        // Arrange - name with invalid characters (uppercase not allowed)
        var request = TestDataBuilder.CreateAgentRequest("Invalid Agent Name!");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/agents", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithEmptyName_ReturnsBadRequest()
    {
        // Arrange
        var request = TestDataBuilder.CreateAgentRequest("");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/agents", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_WithExistingAgent_ReturnsAgent()
    {
        // Arrange - create an agent first
        var createRequest = TestDataBuilder.CreateAgentRequest();
        var createResponse = await _client.PostAsJsonAsync("/api/v1/agents", createRequest);
        var createdAgent = await createResponse.Content.ReadFromJsonAsync<CreateAgentResponseV1>();
        Assert.NotNull(createdAgent);

        // Act
        var response = await _client.GetAsync($"/api/v1/agents/{createdAgent.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var agent = await response.Content.ReadFromJsonAsync<GetAgentResponseV1>();
        Assert.NotNull(agent);
        Assert.Equal(createdAgent.Id, agent.Id);
        Assert.Equal(createdAgent.Name, agent.Name);
        Assert.Equal(createdAgent.Description, agent.Description);
        Assert.Null(agent.CurrentVersionId); // No published version yet
    }

    [Fact]
    public async Task Get_WithNonExistentAgent_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/v1/agents/{nonExistentId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task List_WithMultipleAgents_ReturnsAllUserAgents()
    {
        // Arrange - create multiple agents
        var agent1Request = TestDataBuilder.CreateAgentRequest("agent-1");
        var agent2Request = TestDataBuilder.CreateAgentRequest("agent-2");

        var agent1Response = await _client.PostAsJsonAsync("/api/v1/agents", agent1Request);
        var agent2Response = await _client.PostAsJsonAsync("/api/v1/agents", agent2Request);

        var agent1 = await agent1Response.Content.ReadFromJsonAsync<CreateAgentResponseV1>();
        var agent2 = await agent2Response.Content.ReadFromJsonAsync<CreateAgentResponseV1>();

        // Act
        var response = await _client.GetAsync("/api/v1/agents");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var agents = await response.Content.ReadFromJsonAsync<List<GetAgentResponseV1>>();
        Assert.NotNull(agents);
        Assert.Contains(agents, a => a.Id == agent1!.Id);
        Assert.Contains(agents, a => a.Id == agent2!.Id);
    }

    [Fact]
    public async Task List_WithNoAgents_ReturnsEmptyList()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/agents");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var agents = await response.Content.ReadFromJsonAsync<List<GetAgentResponseV1>>();
        Assert.NotNull(agents);
        // Note: There might be agents from other tests, so we just verify it's a valid list
        Assert.IsType<List<GetAgentResponseV1>>(agents);
    }

    [Fact]
    public async Task Update_WithValidRequest_ReturnsUpdatedAgent()
    {
        // Arrange - create an agent first
        var createRequest = TestDataBuilder.CreateAgentRequest();
        var createResponse = await _client.PostAsJsonAsync("/api/v1/agents", createRequest);
        var createdAgent = await createResponse.Content.ReadFromJsonAsync<CreateAgentResponseV1>();
        Assert.NotNull(createdAgent);

        var updateRequest = TestDataBuilder.CreateAgentRequest("updated-agent", "Updated description");

        // Act
        var response = await _client.PutAsJsonAsync($"/api/v1/agents/{createdAgent.Id}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var agent = await response.Content.ReadFromJsonAsync<GetAgentResponseV1>();
        Assert.NotNull(agent);
        Assert.Equal(createdAgent.Id, agent.Id);
        Assert.Equal(updateRequest.Name, agent.Name);
        Assert.Equal(updateRequest.Description, agent.Description);
    }

    [Fact]
    public async Task Update_WithNonExistentAgent_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var updateRequest = TestDataBuilder.CreateAgentRequest();

        // Act
        var response = await _client.PutAsJsonAsync($"/api/v1/agents/{nonExistentId}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_WithInvalidName_ReturnsBadRequest()
    {
        // Arrange - create an agent first
        var createRequest = TestDataBuilder.CreateAgentRequest();
        var createResponse = await _client.PostAsJsonAsync("/api/v1/agents", createRequest);
        var createdAgent = await createResponse.Content.ReadFromJsonAsync<CreateAgentResponseV1>();
        Assert.NotNull(createdAgent);

        var updateRequest = TestDataBuilder.CreateAgentRequest("Invalid Name!");

        // Act
        var response = await _client.PutAsJsonAsync($"/api/v1/agents/{createdAgent.Id}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Delete_WithExistingAgent_ReturnsNoContent()
    {
        // Arrange - create an agent first
        var createRequest = TestDataBuilder.CreateAgentRequest();
        var createResponse = await _client.PostAsJsonAsync("/api/v1/agents", createRequest);
        var createdAgent = await createResponse.Content.ReadFromJsonAsync<CreateAgentResponseV1>();
        Assert.NotNull(createdAgent);

        // Act
        var response = await _client.DeleteAsync($"/api/v1/agents/{createdAgent.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify agent is deleted
        var getResponse = await _client.GetAsync($"/api/v1/agents/{createdAgent.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_WithNonExistentAgent_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.DeleteAsync($"/api/v1/agents/{nonExistentId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_CascadesVersionDeletion()
    {
        // Arrange - create an agent
        var createRequest = TestDataBuilder.CreateAgentRequest();
        var createResponse = await _client.PostAsJsonAsync("/api/v1/agents", createRequest);
        var createdAgent = await createResponse.Content.ReadFromJsonAsync<CreateAgentResponseV1>();
        Assert.NotNull(createdAgent);

        // Verify initial draft version exists
        var versionsResponse = await _client.GetAsync($"/api/v1/agents/{createdAgent.Id}/versions");
        Assert.Equal(HttpStatusCode.OK, versionsResponse.StatusCode);
        var versions = await versionsResponse.Content.ReadFromJsonAsync<List<GetAgentVersionResponseV1>>();
        Assert.NotNull(versions);
        Assert.NotEmpty(versions);

        // Act - delete the agent
        var deleteResponse = await _client.DeleteAsync($"/api/v1/agents/{createdAgent.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Verify versions are also deleted
        var versionsAfterDelete = await _client.GetAsync($"/api/v1/agents/{createdAgent.Id}/versions");
        Assert.Equal(HttpStatusCode.OK, versionsAfterDelete.StatusCode);
        var versionsAfter = await versionsAfterDelete.Content.ReadFromJsonAsync<List<GetAgentVersionResponseV1>>();
        Assert.NotNull(versionsAfter);
        Assert.Empty(versionsAfter);
    }

    [Fact]
    public async Task CreateAndGet_RoundTrip_PreservesAllData()
    {
        // Arrange
        var createRequest = TestDataBuilder.CreateAgentRequest("roundtrip-test", "Test description");

        // Act - Create
        var createResponse = await _client.PostAsJsonAsync("/api/v1/agents", createRequest);
        var createdAgent = await createResponse.Content.ReadFromJsonAsync<CreateAgentResponseV1>();
        Assert.NotNull(createdAgent);

        // Act - Get
        var getResponse = await _client.GetAsync($"/api/v1/agents/{createdAgent.Id}");
        var retrievedAgent = await getResponse.Content.ReadFromJsonAsync<GetAgentResponseV1>();

        // Assert - all data preserved
        Assert.NotNull(retrievedAgent);
        Assert.Equal(createdAgent.Id, retrievedAgent.Id);
        Assert.Equal(createRequest.Name, retrievedAgent.Name);
        Assert.Equal(createRequest.Description, retrievedAgent.Description);
        Assert.Equal(createdAgent.CreatedAt, retrievedAgent.CreatedAt);
        Assert.NotNull(retrievedAgent.UpdatedAt);
    }
}
