using System.Text.Json;
using System.Text.RegularExpressions;
using DonkeyWork.Agents.A2a.Contracts.Models;

namespace DonkeyWork.Agents.A2a.Contracts.Helpers;

public static partial class A2aProtocolHelper
{
    public static string SanitizeToolName(string name)
    {
        var sanitized = ToolNameSanitizer().Replace(name.ToLowerInvariant(), "_");
        sanitized = sanitized.Trim('_');
        if (sanitized.Length == 0)
            sanitized = "a2a_agent";
        return $"a2a_{sanitized}";
    }

    public static string BuildToolDescription(A2aAgentCardV1? card, string? fallbackDescription)
    {
        var desc = card?.Description ?? fallbackDescription ?? "Remote A2A agent";
        var parts = new List<string> { desc };

        if (card?.Skills is { Count: > 0 })
        {
            parts.Add("Skills:");
            foreach (var skill in card.Skills)
            {
                var line = $"- {skill.Name}";
                if (!string.IsNullOrEmpty(skill.Description) &&
                    !string.Equals(skill.Description, skill.Name, StringComparison.OrdinalIgnoreCase))
                    line += $": {skill.Description}";
                parts.Add(line);
            }
        }

        parts.Add("Returns a contextId for multi-turn conversations.");
        return string.Join("\n", parts);
    }

    public static string BuildMessageSendRequest(string message, string? contextId)
    {
        var id = Guid.NewGuid().ToString("N");
        var messageId = Guid.NewGuid().ToString("N");

        object messageObj = string.IsNullOrEmpty(contextId)
            ? new
            {
                messageId,
                role = "user",
                parts = new[] { new { kind = "text", text = message } },
            }
            : new
            {
                messageId,
                role = "user",
                parts = new[] { new { kind = "text", text = message } },
                contextId,
            };

        var request = new
        {
            jsonrpc = "2.0",
            id,
            method = "message/send",
            @params = new { message = messageObj },
        };

        return JsonSerializer.Serialize(request);
    }

    public static (bool IsError, string Content) ParseMessageResponse(string responseBody)
    {
        using var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;

        if (root.TryGetProperty("error", out var errorProp))
        {
            var errorMessage = errorProp.TryGetProperty("message", out var msgProp)
                ? msgProp.GetString() ?? "Unknown error"
                : "Unknown JSON-RPC error";
            return (true, errorMessage);
        }

        if (!root.TryGetProperty("result", out var result))
            return (true, "A2A server returned no result.");

        var responseContextId = result.TryGetProperty("contextId", out var ctxProp)
            ? ctxProp.GetString()
            : null;

        var textParts = new List<string>();
        if (result.TryGetProperty("parts", out var partsArray) && partsArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var part in partsArray.EnumerateArray())
            {
                var kind = part.TryGetProperty("kind", out var kindProp) ? kindProp.GetString() : null;
                if (kind == "text" && part.TryGetProperty("text", out var textProp))
                    textParts.Add(textProp.GetString() ?? "");
            }
        }

        var responseText = textParts.Count > 0
            ? string.Join("\n", textParts)
            : "Agent returned no text content.";

        var content = !string.IsNullOrEmpty(responseContextId)
            ? $"contextId: {responseContextId}\n\n{responseText}"
            : responseText;

        return (false, content);
    }

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex ToolNameSanitizer();
}
