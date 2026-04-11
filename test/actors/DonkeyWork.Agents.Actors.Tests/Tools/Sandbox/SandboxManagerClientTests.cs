using System.Net;
using System.Text;
using System.Text.Json;
using DonkeyWork.Agents.Actors.Core.Tools.Sandbox;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DonkeyWork.Agents.Actors.Tests.Tools.Sandbox;

public class SandboxManagerClientTests
{
    private const string BaseUrl = "http://sandbox-manager.test";

    #region ExecuteCommandAsync — Success Path

    [Fact]
    public async Task ExecuteCommandAsync_SimpleSuccess_ReturnsOutputAndZeroExitCode()
    {
        var (client, _) = CreateClient(McpToolsCallResponse(text: "hello world\n", isError: false));

        var result = await client.ExecuteCommandAsync("pod-abc", "echo hello world", 60, CancellationToken.None);

        Assert.Equal("hello world", result.Stdout.Trim());
        Assert.Equal(string.Empty, result.Stderr);
        Assert.Equal(0, result.ExitCode);
        Assert.False(result.TimedOut);
        Assert.Equal(0, result.Pid);
    }

    [Fact]
    public async Task ExecuteCommandAsync_SendsRequestToMcpEndpointWithSandboxIdHeader()
    {
        var (client, getRequest) = CreateClient(McpToolsCallResponse(text: "ok", isError: false));

        await client.ExecuteCommandAsync("pod-xyz", "ls", 30, CancellationToken.None);

        var captured = getRequest();
        Assert.NotNull(captured);
        Assert.Equal(HttpMethod.Post, captured!.Method);
        Assert.Equal("/mcp", captured.RequestUri!.AbsolutePath);
        Assert.True(captured.Headers.Contains("x-sandbox-id"));
        Assert.Equal("pod-xyz", captured.Headers.GetValues("x-sandbox-id").Single());
    }

    [Fact]
    public async Task ExecuteCommandAsync_SendsJsonRpcToolsCallWithBashToolAndArguments()
    {
        var (client, getRequest) = CreateClient(McpToolsCallResponse(text: "ok", isError: false));

        await client.ExecuteCommandAsync("pod-1", "uptime", 90, CancellationToken.None);

        var body = await getRequest()!.Content!.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        Assert.Equal("2.0", root.GetProperty("jsonrpc").GetString());
        Assert.Equal("tools/call", root.GetProperty("method").GetString());
        var paramsEl = root.GetProperty("params");
        Assert.Equal("bash", paramsEl.GetProperty("name").GetString());
        var args = paramsEl.GetProperty("arguments");
        Assert.Equal("uptime", args.GetProperty("command").GetString());
        Assert.Equal(90, args.GetProperty("timeoutSeconds").GetInt32());
    }

    #endregion

    #region ExecuteCommandAsync — Exit Code Parsing

    [Fact]
    public async Task ExecuteCommandAsync_NonZeroExitCode_ParsesExitCodeAndStripsMarker()
    {
        var output = "command failed\n[Exit code: 42]";
        var (client, _) = CreateClient(McpToolsCallResponse(text: output, isError: false));

        var result = await client.ExecuteCommandAsync("pod-1", "exit 42", 30, CancellationToken.None);

        Assert.Equal(42, result.ExitCode);
        Assert.Equal("command failed", result.Stdout);
        Assert.False(result.TimedOut);
    }

    [Fact]
    public async Task ExecuteCommandAsync_NegativeExitCode_ParsesCorrectly()
    {
        var (client, _) = CreateClient(McpToolsCallResponse(text: "[Exit code: -1]", isError: false));

        var result = await client.ExecuteCommandAsync("pod-1", "test", 30, CancellationToken.None);

        Assert.Equal(-1, result.ExitCode);
    }

    [Fact]
    public async Task ExecuteCommandAsync_NoExitMarker_ReturnsZeroOnSuccess()
    {
        var (client, _) = CreateClient(McpToolsCallResponse(text: "ok", isError: false));

        var result = await client.ExecuteCommandAsync("pod-1", "true", 30, CancellationToken.None);

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("ok", result.Stdout);
    }

    #endregion

    #region ExecuteCommandAsync — Timeout Parsing

    [Fact]
    public async Task ExecuteCommandAsync_TimeoutMarker_SetsTimedOutAndExtractsPid()
    {
        var output = "partial output\n\n[Operation timed out after 60s. Process PID: 12345]\n[Use the 'resume' tool with pid=12345 to reconnect and retrieve remaining output]";
        var (client, _) = CreateClient(McpToolsCallResponse(text: output, isError: false));

        var result = await client.ExecuteCommandAsync("pod-1", "sleep 100", 60, CancellationToken.None);

        Assert.True(result.TimedOut);
        Assert.Equal(12345, result.Pid);
        Assert.Equal(-1, result.ExitCode);
        Assert.Contains("partial output", result.Stdout);
    }

    #endregion

    #region ExecuteCommandAsync — Error Path

    [Fact]
    public async Task ExecuteCommandAsync_ToolResponseIsError_ReturnsExitCodeMinusOne()
    {
        var (client, _) = CreateClient(McpToolsCallResponse(text: "Error: command must not be empty", isError: true));

        var result = await client.ExecuteCommandAsync("pod-1", "", 30, CancellationToken.None);

        Assert.Equal(-1, result.ExitCode);
        Assert.Contains("Error", result.Stdout);
    }

    [Fact]
    public async Task ExecuteCommandAsync_JsonRpcError_PropagatesErrorMessage()
    {
        var jsonRpcError = """
            {"jsonrpc":"2.0","id":"1","error":{"code":-32602,"message":"Invalid params"}}
            """;
        var (client, _) = CreateClient(JsonResponse(jsonRpcError));

        var result = await client.ExecuteCommandAsync("pod-1", "test", 30, CancellationToken.None);

        Assert.Equal(-1, result.ExitCode);
        Assert.Contains("Invalid params", result.Stdout);
    }

    [Fact]
    public async Task ExecuteCommandAsync_HttpErrorResponse_ThrowsHttpRequestException()
    {
        var (client, _) = CreateClient(new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("upstream failure"),
        });

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            client.ExecuteCommandAsync("pod-1", "test", 30, CancellationToken.None));
    }

    #endregion

    #region ExecuteCommandAsync — SSE Framing

    [Fact]
    public async Task ExecuteCommandAsync_SseFramedResponse_ParsesContentCorrectly()
    {
        // Some MCP server transports wrap the JSON-RPC response in SSE framing
        // (event: message / data: {json}). The parser must strip the framing.
        var jsonRpcBody = JsonRpcResultJson(text: "ok from sse", isError: false);
        var sseFramed = $"event: message\ndata: {jsonRpcBody}\n\n";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(sseFramed, Encoding.UTF8, "text/event-stream"),
        };
        var (client, _) = CreateClient(response);

        var result = await client.ExecuteCommandAsync("pod-1", "echo", 30, CancellationToken.None);

        Assert.Equal("ok from sse", result.Stdout);
        Assert.Equal(0, result.ExitCode);
    }

    #endregion

    #region Test Helpers

    private static (SandboxManagerClient Client, Func<HttpRequestMessage?> GetRequest) CreateClient(HttpResponseMessage response)
    {
        var capturedRequests = new List<HttpRequestMessage>();
        var handler = new CapturingHandler(response, capturedRequests.Add);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) };
        var client = new SandboxManagerClient(httpClient, Mock.Of<ILogger<SandboxManagerClient>>());
        return (client, () => capturedRequests.LastOrDefault());
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;
        private readonly Action<HttpRequestMessage> _onRequest;

        public CapturingHandler(HttpResponseMessage response, Action<HttpRequestMessage> onRequest)
        {
            _response = response;
            _onRequest = onRequest;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _onRequest(request);
            return Task.FromResult(_response);
        }
    }

    private static HttpResponseMessage McpToolsCallResponse(string text, bool isError)
    {
        var json = JsonRpcResultJson(text, isError);
        return JsonResponse(json);
    }

    private static string JsonRpcResultJson(string text, bool isError)
    {
        return JsonSerializer.Serialize(new
        {
            jsonrpc = "2.0",
            id = "1",
            result = new
            {
                content = new[] { new { type = "text", text } },
                isError,
            },
        });
    }

    private static HttpResponseMessage JsonResponse(string json)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };
    }

    #endregion
}
