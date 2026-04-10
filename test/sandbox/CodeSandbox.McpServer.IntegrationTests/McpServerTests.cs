using System.Net.Http.Json;
using System.Text.Json;
using CodeSandbox.Contracts.Requests;
using CodeSandbox.Contracts.Responses;
using Xunit;

namespace CodeSandbox.McpServer.IntegrationTests;

[Trait("Category", "Integration")]
public class McpServerTests : IClassFixture<ServerFixture>
{
    private readonly ServerFixture _fixture;

    public McpServerTests(ServerFixture fixture)
    {
        _fixture = fixture;
    }

    #region GetStatus Tests

    [Fact]
    public async Task GetStatus_BeforeStart_ReturnsIdle()
    {
        var response = await _fixture.HttpClient.GetFromJsonAsync<McpStatusResponse>("/api/mcp/status");

        Assert.Equal("Idle", response?.State);
    }

    #endregion

    #region Start Tests

    [Fact]
    public async Task Start_WithValidCommand_StreamsEventsAndBecomesReady()
    {
        var request = new McpStartRequest
        {
            Command = "npx",
            Arguments = ["-y", "@modelcontextprotocol/server-everything"],
            TimeoutSeconds = 60,
        };

        var events = await StartServerAndCollectEventsAsync(request);

        Assert.Contains(events, e => e.GetProperty("eventType").GetString() == "ready");

        var status = await _fixture.HttpClient.GetFromJsonAsync<McpStatusResponse>("/api/mcp/status");
        Assert.Equal("Ready", status?.State);

        await _fixture.HttpClient.DeleteAsync("/api/mcp");
    }

    [Fact]
    public async Task Start_WithInvalidCommand_StreamsErrorEvent()
    {
        var request = new McpStartRequest
        {
            Command = "/nonexistent/command",
            TimeoutSeconds = 5,
        };

        var events = await StartServerAndCollectEventsAsync(request);

        Assert.Contains(events, e => e.GetProperty("eventType").GetString() == "error");

        var status = await _fixture.HttpClient.GetFromJsonAsync<McpStatusResponse>("/api/mcp/status");
        Assert.Equal("Error", status?.State);

        await _fixture.HttpClient.DeleteAsync("/api/mcp");
    }

    #endregion

    #region ProxyRequest Tests

    [Fact]
    public async Task ProxyRequest_ToolsList_ReturnsTools()
    {
        await StartServerAsync();

        var toolsListRequest = JsonSerializer.Serialize(new
        {
            jsonrpc = "2.0",
            id = "test-1",
            method = "tools/list",
            @params = new { }
        });

        var response = await _fixture.HttpClient.PostAsJsonAsync("/api/mcp/proxy", new McpProxyRequest
        {
            Body = toolsListRequest,
            TimeoutSeconds = 30,
        });
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<McpProxyResponse>();

        Assert.False(result?.IsNotification);
        Assert.NotEmpty(result?.Body ?? "");

        using var doc = JsonDocument.Parse(result!.Body);
        Assert.True(doc.RootElement.TryGetProperty("result", out var resultProp));
        Assert.True(resultProp.TryGetProperty("tools", out var tools));
        Assert.True(tools.GetArrayLength() > 0);

        await _fixture.HttpClient.DeleteAsync("/api/mcp");
    }

    [Fact]
    public async Task ProxyRequest_Notification_ReturnsIsNotificationTrue()
    {
        await StartServerAsync();

        var notification = JsonSerializer.Serialize(new
        {
            jsonrpc = "2.0",
            method = "notifications/initialized"
        });

        var response = await _fixture.HttpClient.PostAsJsonAsync("/api/mcp/proxy", new McpProxyRequest
        {
            Body = notification,
            TimeoutSeconds = 5,
        });
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<McpProxyResponse>();

        Assert.True(result?.IsNotification);

        await _fixture.HttpClient.DeleteAsync("/api/mcp");
    }

    #endregion

    #region Stop Tests

    [Fact]
    public async Task Stop_AfterStart_ReturnsToIdle()
    {
        await StartServerAsync();

        var stopResponse = await _fixture.HttpClient.DeleteAsync("/api/mcp");
        stopResponse.EnsureSuccessStatusCode();

        var status = await _fixture.HttpClient.GetFromJsonAsync<McpStatusResponse>("/api/mcp/status");
        Assert.Equal("Idle", status?.State);
    }

    [Fact]
    public async Task Stop_ThenRestart_Works()
    {
        await StartServerAsync();
        await _fixture.HttpClient.DeleteAsync("/api/mcp");

        await StartServerAsync();

        var status = await _fixture.HttpClient.GetFromJsonAsync<McpStatusResponse>("/api/mcp/status");
        Assert.Equal("Ready", status?.State);

        await _fixture.HttpClient.DeleteAsync("/api/mcp");
    }

    #endregion

    #region PreExecScript Tests

    [Fact]
    public async Task Start_WithPreExecScript_StreamsScriptEvents()
    {
        var request = new McpStartRequest
        {
            Command = "npx",
            Arguments = ["-y", "@modelcontextprotocol/server-everything"],
            PreExecScripts = ["echo 'pre-exec test'"],
            TimeoutSeconds = 60,
        };

        var events = await StartServerAndCollectEventsAsync(request);

        Assert.Contains(events, e => e.GetProperty("eventType").GetString() == "pre_exec_start");
        Assert.Contains(events, e => e.GetProperty("eventType").GetString() == "pre_exec_complete");
        Assert.Contains(events, e => e.GetProperty("eventType").GetString() == "ready");

        await _fixture.HttpClient.DeleteAsync("/api/mcp");
    }

    #endregion

    private async Task StartServerAsync()
    {
        var request = new McpStartRequest
        {
            Command = "npx",
            Arguments = ["-y", "@modelcontextprotocol/server-everything"],
            TimeoutSeconds = 60,
        };

        await StartServerAndCollectEventsAsync(request);
    }

    private async Task<List<JsonElement>> StartServerAndCollectEventsAsync(McpStartRequest request)
    {
        var response = await _fixture.HttpClient.PostAsJsonAsync("/api/mcp/start", request);
        response.EnsureSuccessStatusCode();

        var events = new List<JsonElement>();
        using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);

        while (true)
        {
            var line = await reader.ReadLineAsync();
            if (line == null)
                break;

            if (!line.StartsWith("data: "))
                continue;

            var json = line["data: ".Length..];
            var doc = JsonDocument.Parse(json);
            events.Add(doc.RootElement.Clone());
        }

        return events;
    }
}
