using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using DonkeyWork.Agents.Actors.Contracts.Messages;
using DonkeyWork.Agents.Actors.Core.Providers.Responses;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Actors.Core.Providers.OpenAi;

/// <summary>OpenAI Responses wire-format client for arbitrary compatible endpoints.</summary>
internal sealed class OpenAiResponsesProvider : IAiProvider
{
    private readonly ILogger<OpenAiResponsesProvider> _logger;
    private readonly HttpClient _httpClient;
    private readonly string? _endpoint;

    public OpenAiResponsesProvider(
        ILogger<OpenAiResponsesProvider> logger,
        IHttpClientFactory httpClientFactory,
        string? endpoint)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient(nameof(OpenAiResponsesProvider));
        _httpClient.Timeout = TimeSpan.FromMinutes(10);
        _endpoint = endpoint;
    }

    public async IAsyncEnumerable<ModelResponseBase> StreamCompletionAsync(
        string systemPrompt,
        IReadOnlyList<InternalMessage> messages,
        IReadOnlyList<InternalToolDefinition>? tools,
        ProviderOptions options,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var endpoint = _endpoint ?? options.Endpoint
            ?? throw new InvalidOperationException("An endpoint is required for an OpenAI Responses custom model.");
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        if (!string.IsNullOrWhiteSpace(options.ApiKey))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);

        var payload = new Dictionary<string, object?>
        {
            ["model"] = options.ModelId,
            ["input"] = MapInput(systemPrompt, messages),
            ["max_output_tokens"] = options.MaxTokens,
            ["stream"] = options.Stream,
            ["store"] = false
        };
        if (tools is { Count: > 0 }) payload["tools"] = tools.Select(MapTool).ToArray();
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException($"OpenAI Responses endpoint returned {(int)response.StatusCode}: {body}", null, response.StatusCode);
        }

        if (!options.Stream)
        {
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            foreach (var item in ParseResponse(document.RootElement)) yield return item;
            yield break;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);
        var hasToolCalls = false;
        while (await reader.ReadLineAsync(ct) is { } line)
        {
            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data:", StringComparison.Ordinal)) continue;
            var data = line[5..].Trim();
            if (data == "[DONE]") break;
            JsonDocument document;
            try { document = JsonDocument.Parse(data); }
            catch (JsonException ex)
            {
                _logger.LogDebug(ex, "Ignoring malformed Responses SSE event");
                continue;
            }
            using (document)
            {
                var root = document.RootElement;
                var type = GetString(root, "type");
                switch (type)
                {
                    case "response.output_text.delta":
                        var delta = GetString(root, "delta");
                        if (!string.IsNullOrEmpty(delta))
                            yield return new ModelResponseTextContent { BlockIndex = GetInt(root, "content_index"), Content = delta };
                        break;
                    case "response.output_item.done":
                        if (root.TryGetProperty("item", out var item) && GetString(item, "type") == "function_call")
                        {
                            hasToolCalls = true;
                            yield return MapToolCall(item, GetInt(root, "output_index"));
                        }
                        break;
                    case "response.completed":
                    case "response.incomplete":
                    case "response.failed":
                        if (root.TryGetProperty("response", out var completed))
                        {
                            if (completed.TryGetProperty("usage", out var usage)) yield return MapUsage(usage);
                            yield return MapMetadata(completed, hasToolCalls);
                        }
                        break;
                }
            }
        }
    }

    private static IEnumerable<ModelResponseBase> ParseResponse(JsonElement response)
    {
        var hasTools = false;
        var blockIndex = 0;
        if (response.TryGetProperty("output", out var output) && output.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in output.EnumerateArray())
            {
                if (GetString(item, "type") == "function_call")
                {
                    hasTools = true;
                    yield return MapToolCall(item, blockIndex++);
                    continue;
                }
                if (!item.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array) continue;
                foreach (var part in content.EnumerateArray())
                {
                    if (GetString(part, "type") != "output_text") continue;
                    var text = GetString(part, "text");
                    if (!string.IsNullOrEmpty(text)) yield return new ModelResponseTextContent { BlockIndex = blockIndex++, Content = text };
                }
            }
        }
        if (response.TryGetProperty("usage", out var usage)) yield return MapUsage(usage);
        yield return hasTools
            ? new ModelResponseMetadata { StopReason = InternalStopReason.ToolUse, Properties = new Dictionary<string, object> { ["provider"] = "openai-responses" } }
            : MapMetadata(response);
    }

    private static object[] MapInput(string systemPrompt, IReadOnlyList<InternalMessage> messages)
    {
        var input = new List<object>();
        if (!string.IsNullOrWhiteSpace(systemPrompt)) input.Add(new { role = "system", content = systemPrompt });
        foreach (var message in messages)
        {
            switch (message)
            {
                case InternalContentMessage content:
                    input.Add(new { role = content.Role == InternalMessageRole.Assistant ? "assistant" : "user", content = content.Content });
                    break;
                case InternalAssistantMessage assistant:
                    if (!string.IsNullOrWhiteSpace(assistant.TextContent)) input.Add(new { role = "assistant", content = assistant.TextContent });
                    foreach (var tool in assistant.ToolUses)
                        input.Add(new { type = "function_call", call_id = tool.Id, name = tool.Name, arguments = tool.Input.GetRawText() });
                    break;
                case InternalToolResultMessage result:
                    input.Add(new { type = "function_call_output", call_id = result.ToolUseId, output = result.Content });
                    break;
            }
        }
        return input.ToArray();
    }

    private static object MapTool(InternalToolDefinition tool) => new
    {
        type = "function",
        name = tool.Name,
        description = tool.Description ?? string.Empty,
        parameters = tool.InputSchema ?? new { type = "object", properties = new { } },
        strict = false
    };

    private static ModelResponseToolCall MapToolCall(JsonElement item, int index)
    {
        var arguments = GetString(item, "arguments");
        JsonElement input;
        try { input = JsonSerializer.Deserialize<JsonElement>(string.IsNullOrWhiteSpace(arguments) ? "{}" : arguments); }
        catch (JsonException) { input = JsonSerializer.Deserialize<JsonElement>("{}"); }
        return new ModelResponseToolCall
        {
            BlockIndex = index,
            ToolUseId = GetString(item, "call_id") ?? GetString(item, "id") ?? Guid.NewGuid().ToString("N"),
            ToolName = GetString(item, "name") ?? "unknown",
            Input = input
        };
    }

    private static ModelResponseUsage MapUsage(JsonElement usage) => new()
    {
        InputTokens = GetInt(usage, "input_tokens"),
        OutputTokens = GetInt(usage, "output_tokens")
    };

    private static ModelResponseMetadata MapMetadata(JsonElement response, bool hasToolCalls = false)
    {
        var status = GetString(response, "status");
        var reason = hasToolCalls ? InternalStopReason.ToolUse : status switch
        {
            "incomplete" => InternalStopReason.Incomplete,
            "failed" => InternalStopReason.Incomplete,
            "cancelled" => InternalStopReason.Cancelled,
            _ => InternalStopReason.EndTurn
        };
        return new ModelResponseMetadata { StopReason = reason, Properties = new Dictionary<string, object> { ["provider"] = "openai-responses" } };
    }

    private static string? GetString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    private static int GetInt(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.TryGetInt32(out var number) ? number : 0;
}
