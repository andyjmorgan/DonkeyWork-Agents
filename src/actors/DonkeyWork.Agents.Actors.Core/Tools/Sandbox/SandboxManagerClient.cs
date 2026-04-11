using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Actors.Core.Tools.Sandbox;

/// <summary>
/// HTTP client for communicating with the CodeSandbox Manager's sandbox endpoints.
/// Handles pod lifecycle (find/create) and command execution.
/// </summary>
public sealed class SandboxManagerClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _httpClient;
    private readonly ILogger<SandboxManagerClient> _logger;

    public SandboxManagerClient(HttpClient httpClient, ILogger<SandboxManagerClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<string?> FindSandboxAsync(string userId, string conversationId, CancellationToken ct)
    {
        _logger.LogDebug("Finding sandbox for UserId={UserId}, ConversationId={ConversationId}", userId, conversationId);

        var response = await _httpClient.PostAsJsonAsync("/api/sandbox/find",
            new { userId, conversationId }, JsonOptions, ct);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogDebug("No existing sandbox found");
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            var allow = response.Content.Headers.Contains("Allow")
                ? string.Join(", ", response.Content.Headers.GetValues("Allow"))
                : "(none)";
            _logger.LogError(
                "FindSandbox failed: Status={StatusCode}, Allow={Allow}, Body={Body}, RequestUri={Uri}",
                (int)response.StatusCode, allow, body, response.RequestMessage?.RequestUri);
        }

        response.EnsureSuccessStatusCode();
        var info = await response.Content.ReadFromJsonAsync<SandboxInfoDto>(JsonOptions, ct);

        if (info is null || !info.IsReady)
        {
            _logger.LogInformation("Found sandbox {PodName} but not ready (Phase={Phase})", info?.Name, info?.Phase);
            return null;
        }

        _logger.LogInformation("Found existing sandbox {PodName}", info.Name);
        return info.Name;
    }

    public async Task<string> CreateSandboxAsync(
        string userId,
        string conversationId,
        Action<string>? onProgress,
        CancellationToken ct,
        IReadOnlyList<string>? dynamicCredentialDomains = null)
    {
        _logger.LogInformation("Creating sandbox for UserId={UserId}, ConversationId={ConversationId}", userId, conversationId);

        var request = new
        {
            userId,
            conversationId,
            dynamicCredentialDomains = dynamicCredentialDomains as IEnumerable<string>,
        };

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/sandbox")
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

            _logger.LogDebug("Sandbox creation event: Type={EventType}, PodName={PodName}", eventType, evtPodName);

            switch (eventType)
            {
                case "ContainerCreatedEvent":
                    onProgress?.Invoke("Sandbox pod created, waiting for ready...");
                    break;
                case "ContainerWaitingEvent":
                    var msg = evt.TryGetProperty("message", out var m) ? m.GetString() : null;
                    onProgress?.Invoke(msg is { Length: > 0 } ? msg : "Waiting for sandbox...");
                    break;
                case "ContainerReadyEvent":
                    podName = evtPodName;
                    var elapsed = evt.TryGetProperty("elapsedSeconds", out var es) ? es.GetDouble() : 0;
                    _logger.LogInformation("Sandbox ready: {PodName} (elapsed: {Elapsed}s)", podName, elapsed);
                    break;
                case "ContainerFailedEvent":
                    failReason = evt.TryGetProperty("reason", out var r) ? r.GetString() : "Unknown failure";
                    _logger.LogWarning("Sandbox creation failed: {Reason}", failReason);
                    break;
            }
        }

        if (podName is not null)
            return podName;

        throw new InvalidOperationException($"Sandbox creation failed: {failReason ?? "unknown reason"}");
    }

    public async Task<CommandResult> ExecuteCommandAsync(
        string sandboxId,
        string command,
        int timeoutSeconds,
        CancellationToken ct)
    {
        _logger.LogInformation("Executing command in sandbox {SandboxId}: {Command} (timeout={Timeout}s)",
            sandboxId, Truncate(command, 100), timeoutSeconds);

        // The Manager exposes bash via its MCP server at /mcp (stateless HTTP transport).
        // We POST a JSON-RPC tools/call message and pass the sandbox pod via x-sandbox-id.
        var jsonRpcRequest = new
        {
            jsonrpc = "2.0",
            id = Guid.NewGuid().ToString("N"),
            method = "tools/call",
            @params = new
            {
                name = "bash",
                arguments = new { command, timeoutSeconds },
            },
        };

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = JsonContent.Create(jsonRpcRequest, options: JsonOptions),
        };
        httpRequest.Headers.Add("x-sandbox-id", sandboxId);
        httpRequest.Headers.Accept.ParseAdd("application/json, text/event-stream");

        var response = await _httpClient.SendAsync(httpRequest, ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError(
                "ExecuteCommand failed: Status={StatusCode}, Body={Body}, SandboxId={SandboxId}",
                (int)response.StatusCode, errorBody, sandboxId);
            response.EnsureSuccessStatusCode();
        }

        var rawBody = await response.Content.ReadAsStringAsync(ct);
        var (output, isError) = ParseMcpToolsCallResponse(rawBody);
        var (exitCode, pid, timedOut, cleaned) = ParseBashOutput(output, isError);

        _logger.LogInformation(
            "Command completed: PID={Pid}, ExitCode={ExitCode}, TimedOut={TimedOut}",
            pid, exitCode, timedOut);

        return new CommandResult(cleaned, string.Empty, exitCode, timedOut, pid);
    }

    /// <summary>
    /// Parses an MCP JSON-RPC tools/call response. Handles both plain JSON and
    /// SSE-framed responses (the MCP server may use either depending on transport).
    /// Returns the concatenated text content and the isError flag.
    /// </summary>
    private static (string Output, bool IsError) ParseMcpToolsCallResponse(string body)
    {
        // SSE framing: lines like "event: message" then "data: {json}". Strip framing.
        if (body.Contains("data:", StringComparison.Ordinal))
        {
            foreach (var line in body.Split('\n'))
            {
                var trimmed = line.TrimEnd('\r');
                if (trimmed.StartsWith("data: ", StringComparison.Ordinal))
                {
                    var json = trimmed["data: ".Length..];
                    if (!string.IsNullOrWhiteSpace(json))
                        return ExtractContentAndError(json);
                }
            }
        }

        return ExtractContentAndError(body);
    }

    private static (string Output, bool IsError) ExtractContentAndError(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.TryGetProperty("error", out var errorProp))
        {
            var message = errorProp.TryGetProperty("message", out var msgProp)
                ? msgProp.GetString() ?? "Unknown JSON-RPC error"
                : "Unknown JSON-RPC error";
            return (message, true);
        }

        if (!root.TryGetProperty("result", out var result))
            return (string.Empty, true);

        var isError = result.TryGetProperty("isError", out var isErrorProp) && isErrorProp.GetBoolean();

        var parts = new List<string>();
        if (result.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in content.EnumerateArray())
            {
                var type = item.TryGetProperty("type", out var typeProp) ? typeProp.GetString() : null;
                if (type == "text" && item.TryGetProperty("text", out var textProp))
                {
                    parts.Add(textProp.GetString() ?? string.Empty);
                }
            }
        }

        return (string.Join("\n", parts), isError);
    }

    /// <summary>
    /// Parses the bash tool output to extract exit code, PID, and timeout status.
    /// The Manager's bash tool returns stdout+stderr combined in a single string,
    /// with markers appended on non-zero exit ("[Exit code: N]") or timeout.
    /// </summary>
    private static (int ExitCode, int Pid, bool TimedOut, string Output) ParseBashOutput(string output, bool isError)
    {
        var pid = 0;
        var timedOut = false;
        var exitCode = isError ? -1 : 0;
        var cleaned = output;

        var timeoutMatch = System.Text.RegularExpressions.Regex.Match(
            output, @"\[Operation timed out after \d+s\. Process PID: (\d+)\]");
        if (timeoutMatch.Success)
        {
            timedOut = true;
            pid = int.Parse(timeoutMatch.Groups[1].Value);
            exitCode = -1;
        }

        var exitMatch = System.Text.RegularExpressions.Regex.Match(output, @"\[Exit code: (-?\d+)\]");
        if (exitMatch.Success)
        {
            exitCode = int.Parse(exitMatch.Groups[1].Value);
            cleaned = output[..exitMatch.Index].TrimEnd();
        }

        return (exitCode, pid, timedOut, cleaned);
    }

    public async Task<bool> DeleteSandboxAsync(string sandboxId, CancellationToken ct)
    {
        _logger.LogInformation("Deleting sandbox {SandboxId}", sandboxId);

        try
        {
            var response = await _httpClient.DeleteAsync($"/api/sandbox/{sandboxId}", ct);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<DeleteResultDto>(JsonOptions, ct);

            if (result?.Success == true)
                _logger.LogInformation("Sandbox {SandboxId} deleted successfully", sandboxId);
            else
                _logger.LogWarning("Sandbox {SandboxId} deletion returned failure: {Message}", sandboxId, result?.Message);

            return result?.Success ?? false;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to delete sandbox {SandboxId}", sandboxId);
            return false;
        }
    }

    /// <summary>
    /// Reads SSE events from an HTTP response stream.
    /// Each "data: {json}" line is parsed as a JsonElement.
    /// </summary>
    private static async IAsyncEnumerable<JsonElement> ReadSseEventsAsync(
        HttpResponseMessage response,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        string? line;
        while ((line = await reader.ReadLineAsync(ct)) is not null && !ct.IsCancellationRequested)
        {
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
                // Skip malformed events
                continue;
            }

            yield return element;
        }
    }

    private static string Truncate(string text, int maxLength)
        => text.Length <= maxLength ? text : text[..maxLength] + "...";

    // Client-side DTOs for deserializing Manager responses
    private sealed record SandboxInfoDto(string Name, string Phase, bool IsReady);
    private sealed record DeleteResultDto(bool Success, string? Message, string? PodName);
}
