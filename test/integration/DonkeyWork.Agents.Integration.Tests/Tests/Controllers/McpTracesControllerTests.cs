using System.Net;
using DonkeyWork.Agents.Common.Contracts.Models.Pagination;
using DonkeyWork.Agents.Integration.Tests.Base;
using DonkeyWork.Agents.Integration.Tests.Infrastructure.Authentication;
using DonkeyWork.Agents.Integration.Tests.Infrastructure.Containers;
using DonkeyWork.Agents.Mcp.Contracts.Models;
using DonkeyWork.Agents.Persistence.Entities.Mcp;

namespace DonkeyWork.Agents.Integration.Tests.Tests.Controllers;

[Trait("Category", "Integration")]
public class McpTracesControllerTests : ControllerIntegrationTestBase
{
    private const string BaseUrl = "/api/v1/mcp-traces";

    public McpTracesControllerTests(InfrastructureFixture infrastructure)
        : base(infrastructure)
    {
    }

    #region List Tests

    [Fact]
    public async Task List_NoTraces_ReturnsEmptyResult()
    {
        // Act
        var result = await GetAsync<PaginatedResponse<McpTraceSummaryV1>>(BaseUrl);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task List_WithTraces_ReturnsPaginatedResults()
    {
        // Arrange
        await SeedTracesAsync(TestUser.Default.UserId, 5);

        // Act
        var result = await GetAsync<PaginatedResponse<McpTraceSummaryV1>>($"{BaseUrl}?offset=0&limit=3");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Items.Count);
        Assert.Equal(5, result.TotalCount);
        Assert.True(result.HasMore);
    }

    [Fact]
    public async Task List_ReturnsTracesOrderedByStartedAtDescending()
    {
        // Arrange
        await SeedTracesAsync(TestUser.Default.UserId, 3);

        // Act
        var result = await GetAsync<PaginatedResponse<McpTraceSummaryV1>>(BaseUrl);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Items.Count);
        for (int i = 0; i < result.Items.Count - 1; i++)
        {
            Assert.True(result.Items[i].StartedAt >= result.Items[i + 1].StartedAt);
        }
    }

    [Fact]
    public async Task List_Pagination_SecondPage()
    {
        // Arrange
        await SeedTracesAsync(TestUser.Default.UserId, 5);

        // Act
        var result = await GetAsync<PaginatedResponse<McpTraceSummaryV1>>($"{BaseUrl}?offset=3&limit=3");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(5, result.TotalCount);
        Assert.False(result.HasMore);
    }

    [Fact]
    public async Task List_MapsSummaryFields()
    {
        // Arrange
        var traceId = await SeedSingleTraceAsync(TestUser.Default.UserId, "tools/call", 200, true, 150);

        // Act
        var result = await GetAsync<PaginatedResponse<McpTraceSummaryV1>>(BaseUrl);

        // Assert
        Assert.NotNull(result);
        var trace = Assert.Single(result.Items);
        Assert.Equal(traceId, trace.Id);
        Assert.Equal("tools/call", trace.Method);
        Assert.Equal(200, trace.HttpStatusCode);
        Assert.True(trace.IsSuccess);
        Assert.Equal(150, trace.DurationMs);
    }

    #endregion

    #region User Isolation Tests

    [Fact]
    public async Task List_OnlyReturnsTracesForCurrentUser()
    {
        // Arrange
        var user1 = TestUser.CreateRandom();
        var user2 = TestUser.CreateRandom();
        await SeedTracesAsync(user1.UserId, 3);
        await SeedTracesAsync(user2.UserId, 2);

        // Act - query as user1
        SetTestUser(user1);
        var result = await GetAsync<PaginatedResponse<McpTraceSummaryV1>>(BaseUrl);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.TotalCount);
    }

    [Fact]
    public async Task GetById_TraceBelongingToAnotherUser_ReturnsNotFound()
    {
        // Arrange
        var user1 = TestUser.CreateRandom();
        var user2 = TestUser.CreateRandom();
        var traceId = await SeedSingleTraceAsync(user1.UserId, "tools/list", 200, true, 50);

        // Act - try to get as user2
        SetTestUser(user2);
        var response = await GetResponseAsync($"{BaseUrl}/{traceId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_ExistingTrace_ReturnsDetail()
    {
        // Arrange
        var traceId = await SeedSingleTraceAsync(
            TestUser.Default.UserId, "tools/call", 200, true, 250,
            jsonRpcId: "req-42",
            requestBody: """{"jsonrpc":"2.0","method":"tools/call","id":"req-42","params":{"name":"my_tool"}}""",
            responseBody: """{"jsonrpc":"2.0","result":{"content":[{"type":"text","text":"ok"}]},"id":"req-42"}""");

        // Act
        var result = await GetAsync<McpTraceDetailV1>($"{BaseUrl}/{traceId}");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(traceId, result.Id);
        Assert.Equal("tools/call", result.Method);
        Assert.Equal("req-42", result.JsonRpcId);
        Assert.Equal(200, result.HttpStatusCode);
        Assert.True(result.IsSuccess);
        Assert.Equal(250, result.DurationMs);
        Assert.Contains("my_tool", result.RequestBody);
        Assert.NotNull(result.ResponseBody);
        Assert.Contains("ok", result.ResponseBody);
        Assert.NotNull(result.CompletedAt);
        Assert.NotEqual(default, result.CreatedAt);
    }

    [Fact]
    public async Task GetById_NonExistent_ReturnsNotFound()
    {
        // Act
        var response = await GetResponseAsync($"{BaseUrl}/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetById_ErrorTrace_IncludesErrorMessage()
    {
        // Arrange
        var traceId = await SeedSingleTraceAsync(
            TestUser.Default.UserId, "tools/call", 500, false, 10,
            errorMessage: "Tool not found");

        // Act
        var result = await GetAsync<McpTraceDetailV1>($"{BaseUrl}/{traceId}");

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsSuccess);
        Assert.Equal(500, result.HttpStatusCode);
        Assert.Equal("Tool not found", result.ErrorMessage);
    }

    #endregion

    #region Seed Helpers

    private async Task SeedTracesAsync(Guid userId, int count)
    {
        await using var dbContext = CreateDbContext();
        var now = DateTimeOffset.UtcNow;

        for (int i = 0; i < count; i++)
        {
            dbContext.McpTraces.Add(new McpTraceEntity
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Method = i % 2 == 0 ? "tools/list" : "tools/call",
                JsonRpcId = i.ToString(),
                RequestBody = $$$"""{"jsonrpc":"2.0","method":"tools/list","id":"{{{i}}}"}""",
                ResponseBody = """{"jsonrpc":"2.0","result":{},"id":"0"}""",
                HttpStatusCode = 200,
                IsSuccess = true,
                StartedAt = now.AddMinutes(-count + i),
                CompletedAt = now.AddMinutes(-count + i).AddMilliseconds(100),
                DurationMs = 100,
                ClientIpAddress = "127.0.0.1",
                CreatedAt = now,
            });
        }

        await dbContext.SaveChangesAsync();
    }

    private async Task<Guid> SeedSingleTraceAsync(
        Guid userId,
        string method,
        int httpStatusCode,
        bool isSuccess,
        int durationMs,
        string? jsonRpcId = null,
        string? requestBody = null,
        string? responseBody = null,
        string? errorMessage = null)
    {
        await using var dbContext = CreateDbContext();
        var now = DateTimeOffset.UtcNow;
        var id = Guid.NewGuid();

        dbContext.McpTraces.Add(new McpTraceEntity
        {
            Id = id,
            UserId = userId,
            Method = method,
            JsonRpcId = jsonRpcId ?? "1",
            RequestBody = requestBody ?? $$$"""{"jsonrpc":"2.0","method":"{{{method}}}","id":"1"}""",
            ResponseBody = responseBody ?? """{"jsonrpc":"2.0","result":{},"id":"1"}""",
            HttpStatusCode = httpStatusCode,
            IsSuccess = isSuccess,
            ErrorMessage = errorMessage,
            DurationMs = durationMs,
            ClientIpAddress = "127.0.0.1",
            UserAgent = "TestClient/1.0",
            StartedAt = now.AddMilliseconds(-durationMs),
            CompletedAt = now,
            CreatedAt = now,
        });

        await dbContext.SaveChangesAsync();
        return id;
    }

    #endregion
}
