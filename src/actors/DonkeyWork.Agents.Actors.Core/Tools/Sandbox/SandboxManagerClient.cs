using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Actors.Core.Tools.Sandbox;

public sealed class SandboxManagerClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SandboxManagerClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    public SandboxManagerClient(HttpClient httpClient, ILogger<SandboxManagerClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Find an existing sandbox for the given user and conversation.
    /// Returns the pod name if found, null otherwise.
    /// </summary>
    public async Task<string?> FindSandboxAsync(string userId, string conversationId, CancellationToken ct)
    {
        var url = $"/api/sandbox/find?userId={Uri.EscapeDataString(userId)}&conversationId={Uri.EscapeDataString(conversationId)}";
        var response = await _httpClient.GetAsync(url, ct);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();

        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        return doc.RootElement.GetProperty("name").GetString();
    }

    /// <summary>
    /// Create a new sandbox for the given user and conversation.
    /// Streams SSE creation events and returns the pod name when ready.
    /// </summary>
    public async Task<string> CreateSandboxAsync(
        string userId,
        string conversationId,
        Action<string>? onProgress,
        CancellationToken ct)
    {
        var request = new { userId, conversationId };
        var response = await _httpClient.PostAsJsonAsync("/api/sandbox/", request, JsonOptions, ct);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        string? podName = null;
        string? failReason = null;

        string? line;
        while ((line = await reader.ReadLineAsync(ct)) is not null)
        {
            if (!line.StartsWith("data: ", StringComparison.Ordinal))
                continue;

            var json = line[6..];

            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                var eventType = root.GetProperty("eventType").GetString();

                switch (eventType)
                {
                    case "ContainerCreatedEvent":
                        onProgress?.Invoke("Sandbox pod created, waiting for ready...");
                        break;

                    case "ContainerWaitingEvent":
                        var message = root.TryGetProperty("message", out var msgProp) ? msgProp.GetString() : null;
                        onProgress?.Invoke(message ?? "Waiting for sandbox...");
                        break;

                    case "ContainerReadyEvent":
                        podName = root.GetProperty("podName").GetString();
                        _logger.LogInformation("Sandbox ready: {PodName}", podName);
                        break;

                    case "ContainerFailedEvent":
                        failReason = root.TryGetProperty("reason", out var reasonProp) ? reasonProp.GetString() : "Unknown failure";
                        _logger.LogWarning("Sandbox creation failed: {Reason}", failReason);
                        break;
                }
            }
            catch (JsonException ex)
            {
                _logger.LogDebug(ex, "Failed to parse SSE event: {Json}", json);
            }
        }

        if (podName is not null)
            return podName;

        throw new InvalidOperationException($"Sandbox creation failed: {failReason ?? "unknown reason"}");
    }

    /// <summary>
    /// Execute a command in the given sandbox.
    /// Reads the SSE stream and returns collected stdout, stderr, exit code, and timeout status.
    /// </summary>
    public async Task<CommandResult> ExecuteCommandAsync(
        string sandboxId,
        string command,
        int timeoutSeconds,
        CancellationToken ct)
    {
        var request = new { command, timeoutSeconds };
        var response = await _httpClient.PostAsJsonAsync($"/api/sandbox/{Uri.EscapeDataString(sandboxId)}/execute", request, JsonOptions, ct);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        var stdout = new StringBuilder();
        var stderr = new StringBuilder();
        var exitCode = -1;
        var timedOut = false;

        string? currentEventType = null;
        string? line;

        while ((line = await reader.ReadLineAsync(ct)) is not null)
        {

            if (line.StartsWith("event: ", StringComparison.Ordinal))
            {
                currentEventType = line[7..].Trim();
                continue;
            }

            if (!line.StartsWith("data: ", StringComparison.Ordinal))
            {
                if (line.Length == 0)
                    currentEventType = null;
                continue;
            }

            var json = line[6..];

            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                // Determine event type from SSE event field or JSON $type discriminator
                var effectiveType = currentEventType
                    ?? (root.TryGetProperty("$type", out var typeProp) ? typeProp.GetString() : null);

                switch (effectiveType)
                {
                    case "OutputEvent":
                        var data = root.TryGetProperty("data", out var dataProp) ? dataProp.GetString() ?? "" : "";
                        var streamType = root.TryGetProperty("stream", out var streamProp) ? streamProp : default;

                        var isStderr = streamType.ValueKind == JsonValueKind.String
                            ? string.Equals(streamType.GetString(), "Stderr", StringComparison.OrdinalIgnoreCase)
                            : streamType.ValueKind == JsonValueKind.Number && streamType.GetInt32() == 1;

                        if (isStderr)
                            stderr.Append(data);
                        else
                            stdout.Append(data);
                        break;

                    case "CompletedEvent":
                        exitCode = root.TryGetProperty("exitCode", out var exitProp) ? exitProp.GetInt32() : -1;
                        timedOut = root.TryGetProperty("timedOut", out var toProp) && toProp.GetBoolean();
                        break;
                }
            }
            catch (JsonException ex)
            {
                _logger.LogDebug(ex, "Failed to parse execution SSE event: {Json}", json);
            }
        }

        return new CommandResult(stdout.ToString(), stderr.ToString(), exitCode, timedOut);
    }

    /// <summary>
    /// Delete a sandbox by pod name.
    /// </summary>
    public async Task<bool> DeleteSandboxAsync(string sandboxId, CancellationToken ct)
    {
        var response = await _httpClient.DeleteAsync($"/api/sandbox/{Uri.EscapeDataString(sandboxId)}", ct);
        return response.IsSuccessStatusCode;
    }
}
