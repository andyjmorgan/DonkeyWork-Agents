using System.Net.Http.Json;
using System.Text;
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
            var allow = response.Headers.Contains("Allow")
                ? string.Join(", ", response.Headers.GetValues("Allow"))
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

        var request = new { command, timeoutSeconds };
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/sandbox/{sandboxId}/execute")
        {
            Content = JsonContent.Create(request, options: JsonOptions),
        };

        var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, ct);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            var allow = response.Headers.Contains("Allow")
                ? string.Join(", ", response.Headers.GetValues("Allow"))
                : "(none)";
            _logger.LogError(
                "ExecuteCommand failed: Status={StatusCode}, Allow={Allow}, Body={Body}, RequestUri={Uri}",
                (int)response.StatusCode, allow, body, response.RequestMessage?.RequestUri);
        }

        response.EnsureSuccessStatusCode();

        var stdout = new StringBuilder();
        var stderr = new StringBuilder();
        var exitCode = -1;
        var timedOut = false;
        var pid = 0;
        var eventCount = 0;

        // Local timeout ensures SSE read completes even if Manager stream hangs.
        // The +30s buffer allows the Manager's gRPC deadline (+15s) and the Executor's
        // internal timeout to fire first.
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds + 30));

        try
        {
            await foreach (var evt in ReadSseEventsAsync(response, timeoutCts.Token))
            {
                eventCount++;
                var eventType = evt.GetProperty("eventType").GetString();

                switch (eventType)
                {
                    case "output":
                        var stream = evt.TryGetProperty("stream", out var s) ? s.GetString() : null;
                        var data = evt.TryGetProperty("data", out var d) ? d.GetString() : "";
                        if (stream == "stderr")
                            stderr.AppendLine(data);
                        else
                            stdout.AppendLine(data);
                        if (evt.TryGetProperty("pid", out var p) && p.GetInt32() > 0)
                            pid = p.GetInt32();
                        break;
                    case "exit":
                        if (evt.TryGetProperty("exitCode", out var ec))
                            exitCode = ec.GetInt32();
                        if (evt.TryGetProperty("pid", out var ep) && ep.GetInt32() > 0)
                            pid = ep.GetInt32();
                        break;
                    case "timeout":
                        timedOut = true;
                        if (evt.TryGetProperty("exitCode", out var tc))
                            exitCode = tc.GetInt32();
                        if (evt.TryGetProperty("pid", out var tp) && tp.GetInt32() > 0)
                            pid = tp.GetInt32();
                        break;
                }
            }
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            // Local timeout fired (not caller cancellation) — treat as timeout
            _logger.LogWarning(
                "SSE read timed out after {Timeout}s for sandbox {SandboxId}",
                timeoutSeconds + 30, sandboxId);
            timedOut = true;
            exitCode = -1;
        }

        _logger.LogInformation(
            "Command completed: PID={Pid}, ExitCode={ExitCode}, TimedOut={TimedOut}, Events={EventCount}",
            pid, exitCode, timedOut, eventCount);

        return new CommandResult(stdout.ToString(), stderr.ToString(), exitCode, timedOut, pid);
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
