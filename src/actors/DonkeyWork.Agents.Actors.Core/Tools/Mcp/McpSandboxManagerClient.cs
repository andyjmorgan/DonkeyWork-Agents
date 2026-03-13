using System.Net.Http.Json;
using System.Text.Json;
using DonkeyWork.Agents.Mcp.Contracts.Models;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Actors.Core.Tools.Mcp;

/// <summary>
/// HTTP client for communicating with the CodeSandbox Manager's MCP server endpoints.
/// Handles pod lifecycle (find/create/start) and JSON-RPC proxying.
/// </summary>
public sealed class McpSandboxManagerClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _httpClient;
    private readonly ILogger<McpSandboxManagerClient> _logger;

    public McpSandboxManagerClient(HttpClient httpClient, ILogger<McpSandboxManagerClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<string> EnsureMcpServerAsync(
        string userId,
        Guid mcpServerConfigId,
        McpStdioConnectionConfigV1 config,
        Action<string>? onProgress,
        CancellationToken ct)
    {
        _logger.LogInformation("Ensuring MCP server for UserId={UserId}, ConfigId={ConfigId}, Command={Command}",
            userId, mcpServerConfigId, config.Command);

        var existingPodName = await FindMcpServerAsync(userId, mcpServerConfigId.ToString(), ct);

        if (existingPodName is not null)
        {
            _logger.LogInformation("Found existing MCP server pod {PodName}", existingPodName);

            var status = await GetMcpStatusAsync(existingPodName, ct);
            _logger.LogInformation("MCP server {PodName} process status: {Status}", existingPodName, status);

            if (status is "Ready" or "Initializing")
                return existingPodName;

            _logger.LogInformation("MCP process not running in {PodName}, starting...", existingPodName);
            onProgress?.Invoke($"Starting MCP process in existing pod {existingPodName}...");
            await StartMcpProcessAsync(existingPodName, config, onProgress, ct);
            return existingPodName;
        }

        _logger.LogInformation("No existing MCP server pod found, creating new for ConfigId={ConfigId}", mcpServerConfigId);
        onProgress?.Invoke($"Creating MCP server pod for '{config.Name}'...");
        var podName = await CreateMcpServerAsync(userId, mcpServerConfigId, config, onProgress, ct);
        return podName;
    }

    public async Task<string?> ProxyMcpRequestAsync(
        string podName,
        string jsonRpcBody,
        int timeoutSeconds,
        CancellationToken ct)
    {
        _logger.LogDebug("Proxying MCP request to {PodName} (timeout={Timeout}s)", podName, timeoutSeconds);

        var response = await _httpClient.PostAsJsonAsync(
            $"/api/mcp/{podName}/proxy",
            new { body = jsonRpcBody, timeoutSeconds },
            JsonOptions, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ProxyResultDto>(JsonOptions, ct);
        var hasBody = result?.Body is { Length: > 0 };

        _logger.LogDebug("MCP proxy response from {PodName}: hasBody={HasBody}", podName, hasBody);
        return hasBody ? result!.Body : null;
    }

    public async Task<string> GetMcpStatusAsync(string podName, CancellationToken ct)
    {
        _logger.LogDebug("Getting MCP status for {PodName}", podName);

        var response = await _httpClient.GetAsync($"/api/mcp/{podName}/status", ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<StatusResultDto>(JsonOptions, ct);
        var state = result?.State is { Length: > 0 } ? result.State : "Unknown";

        _logger.LogDebug("MCP status for {PodName}: {State}", podName, state);
        return state;
    }

    private async Task<string?> FindMcpServerAsync(string userId, string mcpServerConfigId, CancellationToken ct)
    {
        _logger.LogDebug("Finding MCP server for UserId={UserId}, ConfigId={ConfigId}", userId, mcpServerConfigId);

        var response = await _httpClient.PostAsJsonAsync("/api/mcp/find",
            new { userId, configId = mcpServerConfigId }, JsonOptions, ct);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogDebug("No existing MCP server found for ConfigId={ConfigId}", mcpServerConfigId);
            return null;
        }

        response.EnsureSuccessStatusCode();
        var info = await response.Content.ReadFromJsonAsync<McpServerInfoDto>(JsonOptions, ct);
        _logger.LogDebug("Found MCP server {PodName}", info?.Name);
        return info?.Name;
    }

    private async Task<string> CreateMcpServerAsync(
        string userId,
        Guid mcpServerConfigId,
        McpStdioConnectionConfigV1 config,
        Action<string>? onProgress,
        CancellationToken ct)
    {
        var request = new
        {
            userId,
            configId = mcpServerConfigId.ToString(),
            command = config.Command,
            args = config.Arguments as IEnumerable<string>,
            preExecScripts = config.PreExecScripts as IEnumerable<string>,
            timeout = 120,
            envVars = config.EnvironmentVariables,
            workDir = config.WorkingDirectory,
        };

        _logger.LogInformation("Creating MCP server pod: Command={Command}, ConfigId={ConfigId}",
            config.Command, mcpServerConfigId);

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/mcp")
        {
            Content = JsonContent.Create(request, options: JsonOptions),
        };

        var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        string? podName = null;
        string? failReason = null;

        await foreach (var evt in ReadSseEventsAsync(response, ct))
        {
            var eventType = evt.GetProperty("eventType").GetString();
            var evtPodName = evt.TryGetProperty("podName", out var pn) ? pn.GetString() : null;

            switch (eventType)
            {
                case "ContainerCreatedEvent":
                    onProgress?.Invoke("MCP server pod created, waiting for ready...");
                    break;
                case "ContainerWaitingEvent":
                    var msg = evt.TryGetProperty("message", out var m) ? m.GetString() : null;
                    onProgress?.Invoke(msg is { Length: > 0 } ? msg : "Waiting for MCP server...");
                    break;
                case "ContainerReadyEvent":
                    podName = evtPodName;
                    break;
                case "McpServerStartedEvent":
                    podName ??= evtPodName;
                    break;
                case "ContainerFailedEvent":
                case "McpServerStartFailedEvent":
                    failReason = evt.TryGetProperty("reason", out var r) ? r.GetString() : "Unknown failure";
                    break;
            }
        }

        if (podName is not null)
            return podName;

        throw new InvalidOperationException($"MCP server creation failed: {failReason ?? "unknown reason"}");
    }

    private async Task StartMcpProcessAsync(
        string podName,
        McpStdioConnectionConfigV1 config,
        Action<string>? onProgress,
        CancellationToken ct)
    {
        var request = new
        {
            command = config.Command,
            args = config.Arguments as IEnumerable<string>,
            preExecScripts = config.PreExecScripts as IEnumerable<string>,
            timeout = 60,
            workDir = config.WorkingDirectory,
        };

        _logger.LogInformation("Starting MCP process in pod {PodName}: Command={Command}", podName, config.Command);

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/mcp/{podName}/start")
        {
            Content = JsonContent.Create(request, options: JsonOptions),
        };

        var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        string? failReason = null;

        await foreach (var evt in ReadSseEventsAsync(response, ct))
        {
            var eventType = evt.GetProperty("eventType").GetString();

            switch (eventType)
            {
                case "ready":
                    _logger.LogInformation("MCP process started successfully in pod {PodName}", podName);
                    return;
                case "error":
                    var error = evt.TryGetProperty("error", out var e) ? e.GetString() : null;
                    var msg = evt.TryGetProperty("message", out var m) ? m.GetString() : null;
                    failReason = error is { Length: > 0 } ? error : msg;
                    break;
                default:
                    if (evt.TryGetProperty("message", out var pm) && pm.GetString() is { Length: > 0 } progressMsg)
                        onProgress?.Invoke(progressMsg);
                    break;
            }
        }

        if (failReason is not null)
            throw new InvalidOperationException($"MCP process start failed: {failReason}");
    }

    private static async IAsyncEnumerable<JsonElement> ReadSseEventsAsync(
        HttpResponseMessage response,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line is null) break;
            if (!line.StartsWith("data: ")) continue;

            var json = line["data: ".Length..];
            if (string.IsNullOrWhiteSpace(json)) continue;

            JsonElement element;
            try
            {
                element = JsonSerializer.Deserialize<JsonElement>(json, JsonOptions);
            }
            catch (JsonException)
            {
                continue;
            }

            yield return element;
        }
    }

    // Client-side DTOs
    private sealed record McpServerInfoDto(string Name, string Phase, bool IsReady);
    private sealed record ProxyResultDto(string? Body, bool IsNotification);
    private sealed record StatusResultDto(string? State, string? Error);
}
