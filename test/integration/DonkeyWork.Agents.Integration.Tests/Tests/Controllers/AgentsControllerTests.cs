using System.Net;
using DonkeyWork.Agents.Orchestrations.Contracts.Models;
using DonkeyWork.Agents.Integration.Tests.Base;
using DonkeyWork.Agents.Integration.Tests.Helpers;
using DonkeyWork.Agents.Integration.Tests.Infrastructure.Authentication;
using DonkeyWork.Agents.Integration.Tests.Infrastructure.Containers;

namespace DonkeyWork.Agents.Integration.Tests.Tests.Controllers;

[Trait("Category", "Integration")]
public class AgentsControllerTests : ControllerIntegrationTestBase
{
    private const string BaseUrl = "/api/v1/orchestrations";

    public AgentsControllerTests(InfrastructureFixture infrastructure)
        : base(infrastructure)
    {
    }

    #region Create Tests

    [Fact]
    public async Task Create_WithValidRequest_ReturnsCreatedAgent()
    {
        // Arrange
        var request = TestDataBuilder.CreateAgentRequest("my-test-agent", "My test agent");

        // Act
        var response = await PostResponseAsync(BaseUrl, request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var agent = await response.Content.ReadFromJsonAsync<CreateOrchestrationResponseV1>(JsonOptions);
        Assert.NotNull(agent);
        Assert.NotEqual(Guid.Empty, agent.Id);
        Assert.Equal(request.Name, agent.Name);
        Assert.Equal(request.Description, agent.Description);
    }

    [Fact]
    public async Task Create_WithInvalidName_ReturnsBadRequest()
    {
        // Arrange - name with uppercase is invalid
        var request = TestDataBuilder.CreateAgentRequest("Invalid-Name");

        // Act
        var response = await PostResponseAsync(BaseUrl, request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithEmptyName_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateOrchestrationRequestV1
        {
            Name = "",
            Description = "Test description"
        };

        // Act
        var response = await PostResponseAsync(BaseUrl, request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region Get Tests

    [Fact]
    public async Task Get_ExistingAgent_ReturnsAgent()
    {
        // Arrange
        var createRequest = TestDataBuilder.CreateAgentRequest();
        var created = await PostAsync<CreateOrchestrationResponseV1>(BaseUrl, createRequest);

        // Act
        var agent = await GetAsync<GetOrchestrationResponseV1>($"{BaseUrl}/{created!.Id}");

        // Assert
        Assert.NotNull(agent);
        Assert.Equal(created.Id, agent.Id);
        Assert.Equal(created.Name, agent.Name);
    }

    [Fact]
    public async Task Get_NonExistingAgent_ReturnsNotFound()
    {
        // Act
        var response = await GetResponseAsync($"{BaseUrl}/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region List Tests

    [Fact]
    public async Task List_ReturnsAllUserAgents()
    {
        // Arrange - use a fresh user to ensure isolation
        SetTestUser(TestUser.CreateRandom());
        await PostAsync<CreateOrchestrationResponseV1>(BaseUrl, TestDataBuilder.CreateAgentRequest("agent-one"));
        await PostAsync<CreateOrchestrationResponseV1>(BaseUrl, TestDataBuilder.CreateAgentRequest("agent-two"));

        // Act
        var agents = await GetAsync<List<GetOrchestrationResponseV1>>(BaseUrl);

        // Assert
        Assert.NotNull(agents);
        Assert.Equal(2, agents.Count);
    }

    [Fact]
    public async Task List_WithNoAgents_ReturnsEmptyList()
    {
        // Arrange - use a fresh user with no agents
        SetTestUser(TestUser.CreateRandom());

        // Act
        var agents = await GetAsync<List<GetOrchestrationResponseV1>>(BaseUrl);

        // Assert
        Assert.NotNull(agents);
        Assert.Empty(agents);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_ExistingAgent_ReturnsUpdatedAgent()
    {
        // Arrange
        var createRequest = TestDataBuilder.CreateAgentRequest("original-name");
        var created = await PostAsync<CreateOrchestrationResponseV1>(BaseUrl, createRequest);

        var updateRequest = TestDataBuilder.CreateAgentRequest("updated-name", "Updated description");

        // Act
        var updated = await PutAsync<GetOrchestrationResponseV1>($"{BaseUrl}/{created!.Id}", updateRequest);

        // Assert
        Assert.NotNull(updated);
        Assert.Equal(created.Id, updated.Id);
        Assert.Equal("updated-name", updated.Name);
        Assert.Equal("Updated description", updated.Description);
    }

    [Fact]
    public async Task Update_NonExistingAgent_ReturnsNotFound()
    {
        // Arrange
        var updateRequest = TestDataBuilder.CreateAgentRequest();

        // Act
        var response = await PutResponseAsync($"{BaseUrl}/{Guid.NewGuid()}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_ExistingAgent_ReturnsNoContent()
    {
        // Arrange
        var createRequest = TestDataBuilder.CreateAgentRequest();
        var created = await PostAsync<CreateOrchestrationResponseV1>(BaseUrl, createRequest);

        // Act
        var response = await DeleteAsync($"{BaseUrl}/{created!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify deletion
        var getResponse = await GetResponseAsync($"{BaseUrl}/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_NonExistingAgent_ReturnsNotFound()
    {
        // Act
        var response = await DeleteAsync($"{BaseUrl}/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_CascadesVersionDeletion()
    {
        // Arrange - Create agent and save a draft version
        var createRequest = TestDataBuilder.CreateAgentRequest();
        var created = await PostAsync<CreateOrchestrationResponseV1>(BaseUrl, createRequest);

        var versionRequest = TestDataBuilder.CreateSaveVersionRequest();
        await PostResponseAsync($"{BaseUrl}/{created!.Id}/versions", versionRequest);

        // Act - Delete the agent
        var response = await DeleteAsync($"{BaseUrl}/{created.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify agent and versions are gone
        var getResponse = await GetResponseAsync($"{BaseUrl}/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);

        // Versions endpoint returns empty list (200 OK) when agent doesn't exist for user
        var versionsResponse = await GetResponseAsync($"{BaseUrl}/{created.Id}/versions");
        Assert.Equal(HttpStatusCode.OK, versionsResponse.StatusCode);
        var versionsContent = await versionsResponse.Content.ReadAsStringAsync();
        Assert.Equal("[]", versionsContent);
    }

    [Fact]
    public async Task CreateAndGet_RoundTrip_PreservesAllData()
    {
        // Arrange
        var request = TestDataBuilder.CreateAgentRequest(
            "round-trip-test",
            "A detailed description for round trip testing");

        // Act - Create
        var createResponse = await PostAsync<CreateOrchestrationResponseV1>(BaseUrl, request);

        // Act - Get
        var getResponse = await GetAsync<GetOrchestrationResponseV1>($"{BaseUrl}/{createResponse!.Id}");

        // Assert - All data preserved
        Assert.NotNull(getResponse);
        Assert.Equal(createResponse.Id, getResponse.Id);
        Assert.Equal(request.Name, getResponse.Name);
        Assert.Equal(request.Description, getResponse.Description);
    }

    #endregion

    #region Update Validation Tests

    [Fact]
    public async Task Update_WithInvalidName_ReturnsBadRequest()
    {
        // Arrange
        var createRequest = TestDataBuilder.CreateAgentRequest();
        var created = await PostAsync<CreateOrchestrationResponseV1>(BaseUrl, createRequest);

        var updateRequest = new CreateOrchestrationRequestV1
        {
            Name = "Invalid-Name-With-Uppercase",
            Description = "Updated description"
        };

        // Act
        var response = await PutResponseAsync($"{BaseUrl}/{created!.Id}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region User Isolation Tests

    [Fact]
    public async Task Get_AgentBelongingToAnotherUser_ReturnsNotFound()
    {
        // Arrange - Create agent as user 1
        var user1 = TestUser.CreateRandom();
        SetTestUser(user1);
        var createRequest = TestDataBuilder.CreateAgentRequest();
        var created = await PostAsync<CreateOrchestrationResponseV1>(BaseUrl, createRequest);

        // Act - Try to get as user 2
        var user2 = TestUser.CreateRandom();
        SetTestUser(user2);
        var response = await GetResponseAsync($"{BaseUrl}/{created!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task List_OnlyReturnsAgentsForCurrentUser()
    {
        // Arrange - Create agents for user 1
        var user1 = TestUser.CreateRandom();
        SetTestUser(user1);
        await PostAsync<CreateOrchestrationResponseV1>(BaseUrl, TestDataBuilder.CreateAgentRequest("user1-agent"));

        var user2 = TestUser.CreateRandom();
        SetTestUser(user2);
        await PostAsync<CreateOrchestrationResponseV1>(BaseUrl, TestDataBuilder.CreateAgentRequest("user2-agent"));

        // Act - List as user 2
        var agents = await GetAsync<List<GetOrchestrationResponseV1>>(BaseUrl);

        // Assert
        Assert.NotNull(agents);
        Assert.Single(agents);
        Assert.Equal("user2-agent", agents[0].Name);
    }

    [Fact]
    public async Task Update_AgentBelongingToAnotherUser_ReturnsNotFound()
    {
        // Arrange - Create agent as user 1
        var user1 = TestUser.CreateRandom();
        SetTestUser(user1);
        var created = await PostAsync<CreateOrchestrationResponseV1>(BaseUrl, TestDataBuilder.CreateAgentRequest());

        // Act - Try to update as user 2
        var user2 = TestUser.CreateRandom();
        SetTestUser(user2);
        var updateRequest = TestDataBuilder.CreateAgentRequest("hacked-name");
        var response = await PutResponseAsync($"{BaseUrl}/{created!.Id}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_AgentBelongingToAnotherUser_ReturnsNotFound()
    {
        // Arrange - Create agent as user 1
        var user1 = TestUser.CreateRandom();
        SetTestUser(user1);
        var created = await PostAsync<CreateOrchestrationResponseV1>(BaseUrl, TestDataBuilder.CreateAgentRequest());

        // Act - Try to delete as user 2
        var user2 = TestUser.CreateRandom();
        SetTestUser(user2);
        var response = await DeleteAsync($"{BaseUrl}/{created!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        // Verify agent still exists for user 1
        SetTestUser(user1);
        var getResponse = await GetResponseAsync($"{BaseUrl}/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
    }

    #endregion
}
