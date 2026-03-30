using System.Text.Json;
using DonkeyWork.Agents.A2a.Contracts.Helpers;
using DonkeyWork.Agents.A2a.Contracts.Models;
using DonkeyWork.Agents.Actors.Core.Providers;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Actors.Core.Tools.A2a;

internal sealed class A2aToolProvider : IAsyncDisposable
{
    private static readonly TimeSpan PerServerTimeout = TimeSpan.FromSeconds(30);

    private readonly Dictionary<string, A2aToolInfo> _tools = new(StringComparer.OrdinalIgnoreCase);
    private HttpClient? _httpClient;

    public async Task InitializeAsync(
        IReadOnlyList<A2aConnectionConfigV1> configs,
        ILogger logger,
        CancellationToken ct)
    {
        _httpClient = new HttpClient();

        foreach (var config in configs)
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(PerServerTimeout);

            try
            {
                var cardUrl = $"{config.Address.TrimEnd('/')}/.well-known/agent-card.json";
                using var request = new HttpRequestMessage(HttpMethod.Get, cardUrl);
                foreach (var (key, value) in config.Headers)
                    request.Headers.TryAddWithoutValidation(key, value);

                var response = await _httpClient.SendAsync(request, timeoutCts.Token);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync(timeoutCts.Token);
                var card = JsonSerializer.Deserialize<A2aAgentCardV1>(json);

                var agentName = card?.Name ?? config.Name;
                var toolName = A2aProtocolHelper.SanitizeToolName(agentName);

                if (_tools.ContainsKey(toolName))
                {
                    logger.LogWarning(
                        "Duplicate A2A tool name '{ToolName}' from server '{ServerName}', skipping",
                        toolName, config.Name);
                    continue;
                }

                var inputSchema = JsonSerializer.Deserialize<JsonElement>("""
                    {
                        "type": "object",
                        "properties": {
                            "message": { "type": "string", "description": "The message to send to the agent" },
                            "contextId": { "type": "string", "description": "Context ID for multi-turn conversations with this agent. Pass the contextId from a previous response to continue the conversation." }
                        },
                        "required": ["message"]
                    }
                    """);

                var definition = new InternalToolDefinition
                {
                    Name = toolName,
                    DisplayName = agentName,
                    Description = A2aProtocolHelper.BuildToolDescription(card, config.Description),
                    InputSchema = inputSchema,
                    DeferLoading = false,
                };

                _tools[toolName] = new A2aToolInfo(definition, config);

                logger.LogInformation(
                    "Connected to A2A server '{ServerName}' at {Address}, registered tool '{ToolName}'",
                    config.Name, config.Address, toolName);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                logger.LogError(
                    "Timed out connecting to A2A server '{ServerName}' at {Address}",
                    config.Name, config.Address);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Failed to connect to A2A server '{ServerName}' at {Address}, skipping",
                    config.Name, config.Address);
            }
        }
    }

    public IReadOnlyList<InternalToolDefinition> GetToolDefinitions()
    {
        return _tools.Values.Select(t => t.Definition).ToList();
    }

    public bool HasTool(string toolName) => _tools.ContainsKey(toolName);

    public string? GetDisplayName(string toolName) =>
        _tools.TryGetValue(toolName, out var info) ? info.Definition.DisplayName : null;

    public async Task<ToolResult> ExecuteAsync(string toolName, JsonElement arguments, CancellationToken ct)
    {
        if (!_tools.TryGetValue(toolName, out var toolInfo))
            return ToolResult.Error($"A2A tool '{toolName}' not found.");

        if (_httpClient is null)
            return ToolResult.Error("A2A tool provider not initialized.");

        try
        {
            var message = arguments.TryGetProperty("message", out var msgProp)
                ? msgProp.GetString()
                : null;

            if (string.IsNullOrEmpty(message))
                return ToolResult.Error("The 'message' parameter is required.");

            var contextId = arguments.TryGetProperty("contextId", out var ctxProp)
                ? ctxProp.GetString()
                : null;

            var jsonRpcRequest = A2aProtocolHelper.BuildMessageSendRequest(message, contextId);
            var endpointUrl = $"{toolInfo.ConnectionConfig.Address.TrimEnd('/')}/a2a";

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpointUrl);
            httpRequest.Content = new StringContent(jsonRpcRequest, System.Text.Encoding.UTF8, "application/json");
            foreach (var (key, value) in toolInfo.ConnectionConfig.Headers)
                httpRequest.Headers.TryAddWithoutValidation(key, value);

            var response = await _httpClient.SendAsync(httpRequest, ct);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync(ct);
            var (isError, content) = A2aProtocolHelper.ParseMessageResponse(responseBody);
            return isError ? ToolResult.Error(content) : ToolResult.Success(content);
        }
        catch (Exception ex)
        {
            return ToolResult.Error($"A2A call failed: {ex.Message}");
        }
    }

    public ValueTask DisposeAsync()
    {
        _httpClient?.Dispose();
        _httpClient = null;
        _tools.Clear();
        return ValueTask.CompletedTask;
    }
}
