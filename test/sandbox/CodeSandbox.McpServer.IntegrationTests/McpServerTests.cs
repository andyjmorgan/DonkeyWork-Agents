using System.Text.Json;
using CodeSandbox.Contracts.Grpc.McpServer;
using Grpc.Core;
using Grpc.Net.Client;
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

    private McpServerService.McpServerServiceClient CreateClient()
    {
        var channel = GrpcChannel.ForAddress(_fixture.ServerUrl);
        return new McpServerService.McpServerServiceClient(channel);
    }

    #region GetStatus Tests

    [Fact]
    public async Task GetStatus_BeforeStart_ReturnsIdle()
    {
        var client = CreateClient();
        var response = await client.GetStatusAsync(new GetStatusRequest());

        Assert.Equal("Idle", response.State);
    }

    #endregion

    #region Start Tests

    [Fact]
    public async Task Start_WithValidCommand_StreamsEventsAndBecomesReady()
    {
        var client = CreateClient();

        var request = new McpStartRequest
        {
            Command = "npx",
            TimeoutSeconds = 60
        };
        request.Arguments.Add("-y");
        request.Arguments.Add("@modelcontextprotocol/server-everything");

        var events = new List<Contracts.Grpc.McpServer.McpStartEvent>();

        using var call = client.Start(request);
        await foreach (var evt in call.ResponseStream.ReadAllAsync())
        {
            events.Add(evt);
        }

        // Should have received events including "ready"
        Assert.Contains(events, e => e.EventType == "ready");

        // Status should now be Ready
        var status = await client.GetStatusAsync(new GetStatusRequest());
        Assert.Equal("Ready", status.State);

        // Clean up
        await client.StopAsync(new StopRequest());
    }

    [Fact]
    public async Task Start_WithInvalidCommand_StreamsErrorEvent()
    {
        var client = CreateClient();

        var request = new McpStartRequest
        {
            Command = "/nonexistent/command",
            TimeoutSeconds = 5
        };

        var events = new List<Contracts.Grpc.McpServer.McpStartEvent>();

        using var call = client.Start(request);
        await foreach (var evt in call.ResponseStream.ReadAllAsync())
        {
            events.Add(evt);
        }

        Assert.Contains(events, e => e.EventType == "error");

        var status = await client.GetStatusAsync(new GetStatusRequest());
        Assert.Equal("Error", status.State);

        // Reset for other tests
        await client.StopAsync(new StopRequest());
    }

    #endregion

    #region ProxyRequest Tests

    [Fact]
    public async Task ProxyRequest_ToolsList_ReturnsTools()
    {
        var client = CreateClient();

        // Start the MCP server
        await StartServerAsync(client);

        // Send tools/list request
        var toolsListRequest = JsonSerializer.Serialize(new
        {
            jsonrpc = "2.0",
            id = "test-1",
            method = "tools/list",
            @params = new { }
        });

        var response = await client.ProxyRequestAsync(new JsonRpcRequest
        {
            Body = toolsListRequest,
            TimeoutSeconds = 30
        });

        Assert.False(response.IsNotification);
        Assert.NotEmpty(response.Body);

        // Parse response to verify it's valid JSON-RPC with tools
        using var doc = JsonDocument.Parse(response.Body);
        Assert.True(doc.RootElement.TryGetProperty("result", out var result));
        Assert.True(result.TryGetProperty("tools", out var tools));
        Assert.True(tools.GetArrayLength() > 0);

        // Clean up
        await client.StopAsync(new StopRequest());
    }

    [Fact]
    public async Task ProxyRequest_Notification_ReturnsIsNotificationTrue()
    {
        var client = CreateClient();

        // Start the MCP server
        await StartServerAsync(client);

        // Send a notification (no id field)
        var notification = JsonSerializer.Serialize(new
        {
            jsonrpc = "2.0",
            method = "notifications/initialized"
        });

        var response = await client.ProxyRequestAsync(new JsonRpcRequest
        {
            Body = notification,
            TimeoutSeconds = 5
        });

        Assert.True(response.IsNotification);

        // Clean up
        await client.StopAsync(new StopRequest());
    }

    #endregion

    #region Stop Tests

    [Fact]
    public async Task Stop_AfterStart_ReturnsToIdle()
    {
        var client = CreateClient();

        await StartServerAsync(client);

        var stopResponse = await client.StopAsync(new StopRequest());
        Assert.True(stopResponse.Success);

        var status = await client.GetStatusAsync(new GetStatusRequest());
        Assert.Equal("Idle", status.State);
    }

    [Fact]
    public async Task Stop_ThenRestart_Works()
    {
        var client = CreateClient();

        // Start, stop, then start again
        await StartServerAsync(client);
        await client.StopAsync(new StopRequest());

        // Should be able to start again
        await StartServerAsync(client);

        var status = await client.GetStatusAsync(new GetStatusRequest());
        Assert.Equal("Ready", status.State);

        await client.StopAsync(new StopRequest());
    }

    #endregion

    #region PreExecScript Tests

    [Fact]
    public async Task Start_WithPreExecScript_StreamsScriptEvents()
    {
        var client = CreateClient();

        var request = new McpStartRequest
        {
            Command = "npx",
            TimeoutSeconds = 60
        };
        request.Arguments.Add("-y");
        request.Arguments.Add("@modelcontextprotocol/server-everything");
        request.PreExecScripts.Add("echo 'pre-exec test'");

        var events = new List<Contracts.Grpc.McpServer.McpStartEvent>();

        using var call = client.Start(request);
        await foreach (var evt in call.ResponseStream.ReadAllAsync())
        {
            events.Add(evt);
        }

        Assert.Contains(events, e => e.EventType == "pre_exec_start");
        Assert.Contains(events, e => e.EventType == "pre_exec_complete");
        Assert.Contains(events, e => e.EventType == "ready");

        await client.StopAsync(new StopRequest());
    }

    #endregion

    private async Task StartServerAsync(McpServerService.McpServerServiceClient client)
    {
        var request = new McpStartRequest
        {
            Command = "npx",
            TimeoutSeconds = 60
        };
        request.Arguments.Add("-y");
        request.Arguments.Add("@modelcontextprotocol/server-everything");

        using var call = client.Start(request);
        await foreach (var _ in call.ResponseStream.ReadAllAsync())
        {
            // Consume all events
        }
    }
}
