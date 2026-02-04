using System.Runtime.CompilerServices;
using DonkeyWork.Agents.Providers.Core.Middleware.Internal;
using DonkeyWork.Agents.Providers.Core.Middleware.Internal.Responses;

namespace DonkeyWork.Agents.Providers.Core.Providers;

/// <summary>
/// Placeholder AI client for testing.
/// </summary>
internal class PlaceholderAiClient : IAiClient
{
    public async IAsyncEnumerable<ModelResponseBase> StreamCompletionAsync(
        IReadOnlyList<InternalMessage> messages,
        IReadOnlyList<InternalToolDefinition>? tools,
        IReadOnlyDictionary<string, object>? providerParameters,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.Delay(10, cancellationToken);

        yield return new ModelResponseBlockStart
        {
            BlockIndex = 0,
            Type = InternalContentBlockType.Text
        };

        var responseText = "This is a placeholder response from the middleware pipeline. ";
        responseText += "Replace this with a real provider implementation.";

        foreach (var chunk in ChunkText(responseText, 10))
        {
            await Task.Delay(5, cancellationToken);
            yield return new ModelResponseTextContent
            {
                BlockIndex = 0,
                Content = chunk
            };
        }

        yield return new ModelResponseBlockEnd
        {
            BlockIndex = 0
        };

        yield return new ModelResponseUsage
        {
            InputTokens = 10,
            OutputTokens = responseText.Split(' ').Length
        };

        yield return new ModelResponseStreamEnd
        {
            Reason = InternalStopReason.EndTurn,
            Metadata = new Dictionary<string, object>
            {
                ["provider"] = "placeholder"
            }
        };
    }

    public async IAsyncEnumerable<ModelResponseBase> CompleteAsync(
        IReadOnlyList<InternalMessage> messages,
        IReadOnlyList<InternalToolDefinition>? tools,
        IReadOnlyDictionary<string, object>? providerParameters,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Simulate non-streaming API call delay
        await Task.Delay(50, cancellationToken);

        var responseText = "This is a placeholder response from the middleware pipeline (non-streaming). ";
        responseText += "Replace this with a real provider implementation.";

        yield return new ModelResponseBlockStart
        {
            BlockIndex = 0,
            Type = InternalContentBlockType.Text
        };

        // Emit complete response at once (non-streaming)
        yield return new ModelResponseTextContent
        {
            BlockIndex = 0,
            Content = responseText
        };

        yield return new ModelResponseBlockEnd
        {
            BlockIndex = 0
        };

        yield return new ModelResponseUsage
        {
            InputTokens = 10,
            OutputTokens = responseText.Split(' ').Length
        };

        yield return new ModelResponseStreamEnd
        {
            Reason = InternalStopReason.EndTurn,
            Metadata = new Dictionary<string, object>
            {
                ["provider"] = "placeholder"
            }
        };
    }

    private static IEnumerable<string> ChunkText(string text, int chunkSize)
    {
        for (var i = 0; i < text.Length; i += chunkSize)
        {
            yield return text.Substring(i, Math.Min(chunkSize, text.Length - i));
        }
    }
}
