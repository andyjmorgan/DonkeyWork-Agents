using System.Text.Json;

namespace DonkeyWork.Agents.Orchestrations.Core.Execution.Helpers;

/// <summary>
/// Parses the rendered Inputs template for TTS nodes. The contract is that the
/// rendered value MUST be a JSON array of strings — even a single-chunk call
/// is expected to render as <c>["text"]</c>.
/// </summary>
public static class TtsInputParser
{
    public static IReadOnlyList<string> Parse(string renderedInputs)
    {
        if (string.IsNullOrWhiteSpace(renderedInputs))
        {
            throw new InvalidOperationException(
                "Inputs template rendered to empty. Expected a JSON array of strings, e.g. [\"your text\"] or {{ Steps.chunk_node.Chunks | to_json }}.");
        }

        try
        {
            using var doc = JsonDocument.Parse(renderedInputs);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidOperationException(
                    $"Inputs template must render to a JSON array of strings. Got a JSON {doc.RootElement.ValueKind} instead.");
            }

            var chunks = new List<string>(doc.RootElement.GetArrayLength());
            foreach (var element in doc.RootElement.EnumerateArray())
            {
                if (element.ValueKind != JsonValueKind.String)
                {
                    throw new InvalidOperationException(
                        $"Inputs array must contain only strings. Got a {element.ValueKind} at index {chunks.Count}.");
                }

                var text = element.GetString();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    chunks.Add(text);
                }
            }

            if (chunks.Count == 0)
            {
                throw new InvalidOperationException("Inputs array is empty after rendering.");
            }

            return chunks;
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"Inputs template must render to a JSON array of strings. Parse error: {ex.Message}",
                ex);
        }
    }
}
