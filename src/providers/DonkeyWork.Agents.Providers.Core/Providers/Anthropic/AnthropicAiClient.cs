using System.Runtime.CompilerServices;
using Anthropic;
using Anthropic.Models.Messages;
using DonkeyWork.Agents.Providers.Core.Middleware;
using DonkeyWork.Agents.Providers.Core.Middleware.Internal;
using DonkeyWork.Agents.Providers.Core.Middleware.Internal.Responses;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Providers.Core.Providers.Anthropic;

/// <summary>
/// Anthropic provider client using the official Anthropic C# SDK (v12+).
/// </summary>
internal sealed class AnthropicAiClient : IAiClient
{
    private readonly AnthropicClient _client;
    private readonly string _modelId;
    private readonly ILogger<AnthropicAiClient> _logger;

    public AnthropicAiClient(string apiKey, string modelId, ILogger<AnthropicAiClient> logger)
    {
        _client = new AnthropicClient { ApiKey = apiKey };
        _modelId = modelId;
        _logger = logger;
    }

    public async IAsyncEnumerable<ModelResponseBase> StreamCompletionAsync(
        IReadOnlyList<InternalMessage> messages,
        IReadOnlyList<InternalToolDefinition>? tools,
        IReadOnlyDictionary<string, object>? providerParameters,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var (systemPrompt, messageParams) = MapMessages(messages);

        var maxTokens = 4096L;
        if (providerParameters?.TryGetValue("max_tokens", out var mt) == true)
            maxTokens = Convert.ToInt64(mt);

        double? temperature = null;
        double? topP = null;

        if (providerParameters is not null)
        {
            if (providerParameters.TryGetValue("temperature", out var temp))
                temperature = Convert.ToDouble(temp);

            if (providerParameters.TryGetValue("top_p", out var tp))
                topP = Convert.ToDouble(tp);
        }

        var parameters = new MessageCreateParams
        {
            Model = _modelId,
            MaxTokens = maxTokens,
            Messages = messageParams,
            System = !string.IsNullOrEmpty(systemPrompt) ? systemPrompt : null,
            Temperature = temperature,
            TopP = topP
        };

        await foreach (var streamEvent in _client.Messages.CreateStreaming(parameters, cancellationToken)
            .WithCancellation(cancellationToken))
        {
            if (streamEvent is null) continue;

            // Content block start
            if (streamEvent.TryPickContentBlockStart(out var blockStart))
            {
                // Check if it's a text block
                if (blockStart.ContentBlock.Type.ToString().Contains("text"))
                {
                    yield return new ModelResponseBlockStart
                    {
                        BlockIndex = (int)blockStart.Index,
                        Type = InternalContentBlockType.Text
                    };
                }
            }
            // Content block delta (text streaming)
            else if (streamEvent.TryPickContentBlockDelta(out var blockDelta))
            {
                if (blockDelta.Delta.TryPickText(out var textDelta))
                {
                    yield return new ModelResponseTextContent
                    {
                        BlockIndex = (int)blockDelta.Index,
                        Content = textDelta.Text
                    };
                }
            }
            // Content block stop
            else if (streamEvent.TryPickContentBlockStop(out var blockStop))
            {
                yield return new ModelResponseBlockEnd { BlockIndex = (int)blockStop.Index };
            }
            // Message delta (usage info)
            else if (streamEvent.TryPickDelta(out var messageDelta))
            {
                if (messageDelta.Usage is not null)
                {
                    yield return new ModelResponseUsage
                    {
                        InputTokens = 0, // Input tokens come from message_start
                        OutputTokens = (int)messageDelta.Usage.OutputTokens
                    };
                }
            }
            // Message start (initial usage)
            else if (streamEvent.TryPickStart(out var messageStart))
            {
                if (messageStart.Message?.Usage is not null)
                {
                    yield return new ModelResponseUsage
                    {
                        InputTokens = (int)messageStart.Message.Usage.InputTokens,
                        OutputTokens = 0
                    };
                }
            }
            // Message stop
            else if (streamEvent.TryPickStop(out _))
            {
                yield return new ModelResponseStreamEnd
                {
                    Reason = InternalStopReason.EndTurn,
                    Metadata = new Dictionary<string, object>
                    {
                        ["provider"] = "anthropic"
                    }
                };
            }
        }
    }

    public async IAsyncEnumerable<ModelResponseBase> CompleteAsync(
        IReadOnlyList<InternalMessage> messages,
        IReadOnlyList<InternalToolDefinition>? tools,
        IReadOnlyDictionary<string, object>? providerParameters,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var (systemPrompt, messageParams) = MapMessages(messages);

        var maxTokens = 4096L;
        if (providerParameters?.TryGetValue("max_tokens", out var mt) == true)
            maxTokens = Convert.ToInt64(mt);

        double? temperature = null;
        double? topP = null;

        if (providerParameters is not null)
        {
            if (providerParameters.TryGetValue("temperature", out var temp))
                temperature = Convert.ToDouble(temp);

            if (providerParameters.TryGetValue("top_p", out var tp))
                topP = Convert.ToDouble(tp);
        }

        var parameters = new MessageCreateParams
        {
            Model = _modelId,
            MaxTokens = maxTokens,
            Messages = messageParams,
            System = !string.IsNullOrEmpty(systemPrompt) ? systemPrompt : null,
            Temperature = temperature,
            TopP = topP
        };

        // Non-streaming API call
        var response = await _client.Messages.Create(parameters, cancellationToken);

        // Emit block start
        yield return new ModelResponseBlockStart
        {
            BlockIndex = 0,
            Type = InternalContentBlockType.Text
        };

        // Extract text content from response
        var textContent = new System.Text.StringBuilder();
        foreach (var block in response.Content)
        {
            if (block.TryPickText(out var textBlock))
            {
                textContent.Append(textBlock.Text);
            }
        }

        // Emit the complete text as a single chunk
        if (textContent.Length > 0)
        {
            yield return new ModelResponseTextContent
            {
                BlockIndex = 0,
                Content = textContent.ToString()
            };
        }

        // Emit block end
        yield return new ModelResponseBlockEnd { BlockIndex = 0 };

        // Emit usage
        if (response.Usage is not null)
        {
            yield return new ModelResponseUsage
            {
                InputTokens = (int)response.Usage.InputTokens,
                OutputTokens = (int)response.Usage.OutputTokens
            };
        }

        // Emit stream end - map StopReason to internal reason
        var stopReason = InternalStopReason.EndTurn;
        var stopReasonStr = response.StopReason?.ToString();
        if (stopReasonStr?.Contains("max_tokens", StringComparison.OrdinalIgnoreCase) == true)
        {
            stopReason = InternalStopReason.MaxTokens;
        }
        else if (stopReasonStr?.Contains("tool_use", StringComparison.OrdinalIgnoreCase) == true)
        {
            stopReason = InternalStopReason.ToolUse;
        }

        yield return new ModelResponseStreamEnd
        {
            Reason = stopReason,
            Metadata = new Dictionary<string, object>
            {
                ["provider"] = "anthropic"
            }
        };
    }

    private static (string? systemPrompt, MessageParam[] messages) MapMessages(
        IReadOnlyList<InternalMessage> messages)
    {
        string? systemPrompt = null;
        var result = new List<MessageParam>();

        foreach (var msg in messages)
        {
            if (msg is not InternalContentMessage contentMsg) continue;

            switch (msg.Role)
            {
                case InternalMessageRole.System:
                    systemPrompt = contentMsg.GetTextContent();
                    break;
                case InternalMessageRole.User:
                    // TODO: Add multimodal support with ContentBlock array
                    result.Add(new MessageParam { Role = Role.User, Content = contentMsg.GetTextContent() });
                    break;
                case InternalMessageRole.Assistant:
                    result.Add(new MessageParam { Role = Role.Assistant, Content = contentMsg.GetTextContent() });
                    break;
            }
        }

        return (systemPrompt, result.ToArray());
    }
}
