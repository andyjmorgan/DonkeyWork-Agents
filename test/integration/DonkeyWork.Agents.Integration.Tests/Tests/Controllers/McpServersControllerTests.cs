using System.Net;
using DonkeyWork.Agents.Common.Contracts.Enums;
using DonkeyWork.Agents.Integration.Tests.Base;
using DonkeyWork.Agents.Integration.Tests.Helpers;
using DonkeyWork.Agents.Integration.Tests.Infrastructure.Authentication;
using DonkeyWork.Agents.Integration.Tests.Infrastructure.Containers;
using DonkeyWork.Agents.Mcp.Contracts.Models;

namespace DonkeyWork.Agents.Integration.Tests.Tests.Controllers;

[Trait("Category", "Integration")]
public class McpServersControllerTests : ControllerIntegrationTestBase
{
    private const string BaseUrl = "/api/v1/mcp-servers";

    public McpServersControllerTests(InfrastructureFixture infrastructure)
        : base(infrastructure)
    {
    }

    #region Create Tests

    [Fact]
    public async Task Create_StdioServer_ReturnsCreatedServer()
    {
        // Arrange
        var request = TestDataBuilder.CreateMcpStdioServerRequest(
            name: "My Stdio Server",
            command: "npx");

        // Act
        var response = await PostResponseAsync(BaseUrl, request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var server = await response.Content.ReadFromJsonAsync<McpServerDetailsV1>(JsonOptions);
        Assert.NotNull(server);
        Assert.NotEqual(Guid.Empty, server.Id);
        Assert.Equal(request.Name, server.Name);
        Assert.Equal(McpTransportType.Stdio, server.TransportType);
        Assert.NotNull(server.StdioConfiguration);
        Assert.Equal("npx", server.StdioConfiguration.Command);
    }

    [Fact]
    public async Task Create_HttpServer_ReturnsCreatedServer()
    {
        // Arrange
        var request = TestDataBuilder.CreateMcpHttpServerRequest(
            name: "My HTTP Server",
            endpoint: "https://api.example.com/mcp",
            transportMode: McpHttpTransportMode.StreamableHttp);

        // Act
        var response = await PostResponseAsync(BaseUrl, request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var server = await response.Content.ReadFromJsonAsync<McpServerDetailsV1>(JsonOptions);
        Assert.NotNull(server);
        Assert.NotEqual(Guid.Empty, server.Id);
        Assert.Equal(request.Name, server.Name);
        Assert.Equal(McpTransportType.Http, server.TransportType);
        Assert.NotNull(server.HttpConfiguration);
        Assert.Equal("https://api.example.com/mcp", server.HttpConfiguration.Endpoint);
        Assert.Equal(McpHttpTransportMode.StreamableHttp, server.HttpConfiguration.TransportMode);
    }

    #endregion

    #region Get Tests

    [Fact]
    public async Task Get_ExistingServer_ReturnsServer()
    {
        // Arrange
        var createRequest = TestDataBuilder.CreateMcpStdioServerRequest();
        var created = await PostAsync<McpServerDetailsV1>(BaseUrl, createRequest);

        // Act
        var server = await GetAsync<McpServerDetailsV1>($"{BaseUrl}/{created!.Id}");

        // Assert
        Assert.NotNull(server);
        Assert.Equal(created.Id, server.Id);
        Assert.Equal(created.Name, server.Name);
    }

    [Fact]
    public async Task Get_NonExistingServer_ReturnsNotFound()
    {
        // Act
        var response = await GetResponseAsync($"{BaseUrl}/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region List Tests

    [Fact]
    public async Task List_ReturnsAllUserServers()
    {
        // Arrange
        await PostAsync<McpServerDetailsV1>(BaseUrl, TestDataBuilder.CreateMcpStdioServerRequest("Server One"));
        await PostAsync<McpServerDetailsV1>(BaseUrl, TestDataBuilder.CreateMcpHttpServerRequest("Server Two"));
        await PostAsync<McpServerDetailsV1>(BaseUrl, TestDataBuilder.CreateMcpStdioServerRequest("Server Three"));

        // Act
        var servers = await GetAsync<List<McpServerSummaryV1>>(BaseUrl);

        // Assert
        Assert.NotNull(servers);
        Assert.Equal(3, servers.Count);
    }

    [Fact]
    public async Task List_WithNoServers_ReturnsEmptyList()
    {
        // Act
        var servers = await GetAsync<List<McpServerSummaryV1>>(BaseUrl);

        // Assert
        Assert.NotNull(servers);
        Assert.Empty(servers);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_ExistingServer_ReturnsUpdatedServer()
    {
        // Arrange
        var createRequest = TestDataBuilder.CreateMcpStdioServerRequest("Original Name");
        var created = await PostAsync<McpServerDetailsV1>(BaseUrl, createRequest);

        var updateRequest = TestDataBuilder.UpdateMcpStdioServerRequest(
            name: "Updated Name",
            command: "python");

        // Act
        var updated = await PutAsync<McpServerDetailsV1>($"{BaseUrl}/{created!.Id}", updateRequest);

        // Assert
        Assert.NotNull(updated);
        Assert.Equal(created.Id, updated.Id);
        Assert.Equal("Updated Name", updated.Name);
        Assert.NotNull(updated.StdioConfiguration);
        Assert.Equal("python", updated.StdioConfiguration.Command);
    }

    [Fact]
    public async Task Update_NonExistingServer_ReturnsNotFound()
    {
        // Arrange
        var updateRequest = TestDataBuilder.UpdateMcpStdioServerRequest();

        // Act
        var response = await PutResponseAsync($"{BaseUrl}/{Guid.NewGuid()}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_ExistingServer_ReturnsNoContent()
    {
        // Arrange
        var createRequest = TestDataBuilder.CreateMcpStdioServerRequest();
        var created = await PostAsync<McpServerDetailsV1>(BaseUrl, createRequest);

        // Act
        var deleteResponse = await DeleteAsync($"{BaseUrl}/{created!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Verify it's actually deleted
        var getResponse = await GetResponseAsync($"{BaseUrl}/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_NonExistingServer_ReturnsNotFound()
    {
        // Act
        var response = await DeleteAsync($"{BaseUrl}/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region User Isolation Tests

    [Fact]
    public async Task Get_ServerBelongingToAnotherUser_ReturnsNotFound()
    {
        // Arrange - Create as user 1
        var user1 = TestUser.CreateRandom();
        SetTestUser(user1);
        var createRequest = TestDataBuilder.CreateMcpStdioServerRequest();
        var created = await PostAsync<McpServerDetailsV1>(BaseUrl, createRequest);

        // Act - Try to get as user 2
        var user2 = TestUser.CreateRandom();
        SetTestUser(user2);
        var response = await GetResponseAsync($"{BaseUrl}/{created!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task List_OnlyReturnsCurrentUserServers()
    {
        // Arrange - Create servers as user 1
        var user1 = TestUser.CreateRandom();
        SetTestUser(user1);
        await PostAsync<McpServerDetailsV1>(BaseUrl, TestDataBuilder.CreateMcpStdioServerRequest("User1 Server 1"));
        await PostAsync<McpServerDetailsV1>(BaseUrl, TestDataBuilder.CreateMcpStdioServerRequest("User1 Server 2"));

        var user2 = TestUser.CreateRandom();
        SetTestUser(user2);
        await PostAsync<McpServerDetailsV1>(BaseUrl, TestDataBuilder.CreateMcpStdioServerRequest("User2 Server 1"));

        // Act - List as user 2
        var servers = await GetAsync<List<McpServerSummaryV1>>(BaseUrl);

        // Assert - Should only see user 2's server
        Assert.NotNull(servers);
        Assert.Single(servers);
        Assert.Equal("User2 Server 1", servers[0].Name);
    }

    #endregion
}
