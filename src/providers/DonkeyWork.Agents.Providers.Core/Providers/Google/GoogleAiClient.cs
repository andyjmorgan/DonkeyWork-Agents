using System.Runtime.CompilerServices;
using DonkeyWork.Agents.Providers.Core.Middleware;
using DonkeyWork.Agents.Providers.Core.Middleware.Internal;
using DonkeyWork.Agents.Providers.Core.Middleware.Internal.Responses;
using GenerativeAI;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Providers.Core.Providers.Google;

/// <summary>
/// Google Gemini provider client using the Google_GenerativeAI SDK.
/// </summary>
internal sealed class GoogleAiClient : IAiClient
{
    private readonly GenerativeModel _model;
    private readonly ILogger<GoogleAiClient> _logger;

    public GoogleAiClient(string apiKey, string modelId, ILogger<GoogleAiClient> logger)
    {
        var googleAi = new GoogleAi(apiKey);
        _model = googleAi.CreateGenerativeModel(modelId);
        _logger = logger;
    }

    public async IAsyncEnumerable<ModelResponseBase> StreamCompletionAsync(
        IReadOnlyList<InternalMessage> messages,
        IReadOnlyList<InternalToolDefinition>? tools,
        IReadOnlyDictionary<string, object>? providerParameters,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ConfigureModel(providerParameters);

        var request = BuildRequest(messages);
        var blockIndex = 0;
        var blockStarted = false;

        await foreach (var response in _model.StreamContentAsync(request, cancellationToken: cancellationToken)
            .WithCancellation(cancellationToken))
        {
            if (response?.Text is not null)
            {
                if (!blockStarted)
                {
                    yield return new ModelResponseBlockStart
                    {
                        BlockIndex = blockIndex,
                        Type = InternalContentBlockType.Text
                    };
                    blockStarted = true;
                }

                yield return new ModelResponseTextContent { Content = response.Text };
            }

            // Usage info
            if (response?.UsageMetadata is not null)
            {
                yield return new ModelResponseUsage
                {
                    InputTokens = response.UsageMetadata.PromptTokenCount ?? 0,
                    OutputTokens = response.UsageMetadata.CandidatesTokenCount ?? 0
                };
            }

            // Check finish reason
            if (response?.Candidates is { Count: > 0 })
            {
                var candidate = response.Candidates[0];
                if (candidate.FinishReason is not null && candidate.FinishReason != FinishReason.Unspecified)
                {
                    if (blockStarted)
                    {
                        yield return new ModelResponseBlockEnd { BlockIndex = blockIndex };
                        blockStarted = false;
                    }

                    yield return new ModelResponseStreamEnd
                    {
                        Reason = MapFinishReason(candidate.FinishReason.Value),
                        Metadata = new Dictionary<string, object>
                        {
                            ["provider"] = "google"
                        }
                    };
                }
            }
        }

        // Close block if still open
        if (blockStarted)
        {
            yield return new ModelResponseBlockEnd { BlockIndex = blockIndex };

            yield return new ModelResponseStreamEnd
            {
                Reason = InternalStopReason.EndTurn,
                Metadata = new Dictionary<string, object>
                {
                    ["provider"] = "google"
                }
            };
        }
    }

    private void ConfigureModel(IReadOnlyDictionary<string, object>? providerParameters)
    {
        if (providerParameters is null) return;

        if (providerParameters.TryGetValue("temperature", out var temp))
            _model.Temperature = Convert.ToSingle(temp);

        if (providerParameters.TryGetValue("max_tokens", out var maxTokens))
            _model.MaxOutputTokens = Convert.ToInt32(maxTokens);

        if (providerParameters.TryGetValue("top_p", out var topP))
            _model.TopP = Convert.ToSingle(topP);

        if (providerParameters.TryGetValue("top_k", out var topK))
            _model.TopK = Convert.ToInt32(topK);
    }

    private static GenerateContentRequest BuildRequest(IReadOnlyList<InternalMessage> messages)
    {
        var request = new GenerateContentRequest();
        string? systemInstruction = null;

        foreach (var msg in messages)
        {
            if (msg is not InternalUserMessage userMsg) continue;

            switch (msg.Role)
            {
                case InternalMessageRole.System:
                    systemInstruction = userMsg.Content;
                    break;
                case InternalMessageRole.User:
                    request.Contents.Add(new Content(userMsg.Content, Roles.User));
                    break;
                case InternalMessageRole.Assistant:
                    request.Contents.Add(new Content(userMsg.Content, Roles.Model));
                    break;
            }
        }

        if (systemInstruction is not null)
        {
            request.SystemInstruction = new Content(systemInstruction, Roles.User);
        }

        return request;
    }

    private static InternalStopReason MapFinishReason(FinishReason reason)
    {
        return reason switch
        {
            FinishReason.Stop => InternalStopReason.EndTurn,
            FinishReason.MaxTokens => InternalStopReason.MaxTokens,
            FinishReason.Safety => InternalStopReason.SafetyStop,
            FinishReason.Recitation => InternalStopReason.Recitation,
            _ => InternalStopReason.EndTurn
        };
    }
}
