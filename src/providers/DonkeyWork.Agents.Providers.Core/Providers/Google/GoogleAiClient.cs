using System.Runtime.CompilerServices;
using DonkeyWork.Agents.Providers.Core.Middleware;
using DonkeyWork.Agents.Providers.Core.Middleware.Internal;
using DonkeyWork.Agents.Providers.Core.Middleware.Internal.Responses;
using GenerativeAI;
using GenerativeAI.Types;
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

                yield return new ModelResponseTextContent
                {
                    BlockIndex = blockIndex,
                    Content = response.Text
                };
            }

            // Usage info
            if (response?.UsageMetadata is not null)
            {
                yield return new ModelResponseUsage
                {
                    InputTokens = response.UsageMetadata.PromptTokenCount,
                    OutputTokens = response.UsageMetadata.CandidatesTokenCount
                };
            }

            // Check finish reason
            if (response?.Candidates is { Length: > 0 })
            {
                var candidate = response.Candidates[0];
                if (candidate.FinishReason is not null &&
                    candidate.FinishReason != FinishReason.FINISH_REASON_UNSPECIFIED)
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

    public async IAsyncEnumerable<ModelResponseBase> CompleteAsync(
        IReadOnlyList<InternalMessage> messages,
        IReadOnlyList<InternalToolDefinition>? tools,
        IReadOnlyDictionary<string, object>? providerParameters,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ConfigureModel(providerParameters);

        var request = BuildRequest(messages);

        // Non-streaming API call
        var response = await _model.GenerateContentAsync(request, cancellationToken: cancellationToken);

        // Emit block start
        yield return new ModelResponseBlockStart
        {
            BlockIndex = 0,
            Type = InternalContentBlockType.Text
        };

        // Extract and emit text content
        if (response?.Text is not null)
        {
            yield return new ModelResponseTextContent
            {
                BlockIndex = 0,
                Content = response.Text
            };
        }

        // Emit block end
        yield return new ModelResponseBlockEnd { BlockIndex = 0 };

        // Emit usage info
        if (response?.UsageMetadata is not null)
        {
            yield return new ModelResponseUsage
            {
                InputTokens = response.UsageMetadata.PromptTokenCount,
                OutputTokens = response.UsageMetadata.CandidatesTokenCount
            };
        }

        // Determine finish reason and emit stream end
        var stopReason = InternalStopReason.EndTurn;
        if (response?.Candidates is { Length: > 0 })
        {
            var candidate = response.Candidates[0];
            if (candidate.FinishReason is not null &&
                candidate.FinishReason != FinishReason.FINISH_REASON_UNSPECIFIED)
            {
                stopReason = MapFinishReason(candidate.FinishReason.Value);
            }
        }

        yield return new ModelResponseStreamEnd
        {
            Reason = stopReason,
            Metadata = new Dictionary<string, object>
            {
                ["provider"] = "google"
            }
        };
    }

    private void ConfigureModel(IReadOnlyDictionary<string, object>? providerParameters)
    {
        if (providerParameters is null) return;

        // Configure via GenerationConfig
        var config = _model.Config ?? new GenerationConfig();

        if (providerParameters.TryGetValue("temperature", out var temp))
            config.Temperature = Convert.ToSingle(temp);

        if (providerParameters.TryGetValue("max_tokens", out var maxTokens))
            config.MaxOutputTokens = Convert.ToInt32(maxTokens);

        if (providerParameters.TryGetValue("top_p", out var topP))
            config.TopP = Convert.ToSingle(topP);

        if (providerParameters.TryGetValue("top_k", out var topK))
            config.TopK = Convert.ToInt32(topK);

        _model.Config = config;
    }

    private static GenerateContentRequest BuildRequest(IReadOnlyList<InternalMessage> messages)
    {
        var request = new GenerateContentRequest();
        string? systemInstruction = null;

        foreach (var msg in messages)
        {
            if (msg is not InternalContentMessage contentMsg) continue;

            switch (msg.Role)
            {
                case InternalMessageRole.System:
                    systemInstruction = contentMsg.GetTextContent();
                    break;
                case InternalMessageRole.User:
                    request.Contents.Add(new Content(contentMsg.GetTextContent(), Roles.User));
                    break;
                case InternalMessageRole.Assistant:
                    request.Contents.Add(new Content(contentMsg.GetTextContent(), Roles.Model));
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
            FinishReason.STOP => InternalStopReason.EndTurn,
            FinishReason.MAX_TOKENS => InternalStopReason.MaxTokens,
            FinishReason.SAFETY => InternalStopReason.SafetyStop,
            FinishReason.RECITATION => InternalStopReason.Recitation,
            _ => InternalStopReason.EndTurn
        };
    }
}
