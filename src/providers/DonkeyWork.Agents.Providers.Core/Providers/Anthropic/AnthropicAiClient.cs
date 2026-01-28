using System.Runtime.CompilerServices;
using System.Text.Json;
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

        var maxTokens = 4096;
        if (providerParameters?.TryGetValue("max_tokens", out var mt) == true)
            maxTokens = Convert.ToInt32(mt);

        var parameters = new MessageCreateParams
        {
            Model = _modelId,
            MaxTokens = maxTokens,
            Messages = messageParams
        };

        if (!string.IsNullOrEmpty(systemPrompt))
        {
            parameters.System = systemPrompt;
        }

        if (providerParameters is not null)
        {
            if (providerParameters.TryGetValue("temperature", out var temp))
                parameters.Temperature = Convert.ToDouble(temp);

            if (providerParameters.TryGetValue("top_p", out var topP))
                parameters.TopP = Convert.ToDouble(topP);
        }

        var blockIndex = 0;
        var textBlockStarted = false;

        await foreach (var streamEvent in _client.Messages.CreateStreaming(parameters)
            .WithCancellation(cancellationToken))
        {
            if (streamEvent is null) continue;

            switch (streamEvent)
            {
                case RawContentBlockStartEvent blockStart:
                    if (blockStart.ContentBlock is TextBlock)
                    {
                        yield return new ModelResponseBlockStart
                        {
                            BlockIndex = blockIndex,
                            Type = InternalContentBlockType.Text
                        };
                        textBlockStarted = true;
                    }
                    break;

                case RawContentBlockDeltaEvent blockDelta:
                    if (blockDelta.Delta is TextDelta textDelta)
                    {
                        yield return new ModelResponseTextContent { Content = textDelta.Text };
                    }
                    break;

                case RawContentBlockStopEvent:
                    if (textBlockStarted)
                    {
                        yield return new ModelResponseBlockEnd { BlockIndex = blockIndex };
                        blockIndex++;
                        textBlockStarted = false;
                    }
                    break;

                case RawMessageDeltaEvent messageDelta:
                    if (messageDelta.Usage is not null)
                    {
                        yield return new ModelResponseUsage
                        {
                            InputTokens = 0, // Input tokens come from message_start
                            OutputTokens = messageDelta.Usage.OutputTokens
                        };
                    }
                    break;

                case RawMessageStartEvent messageStart:
                    if (messageStart.Message?.Usage is not null)
                    {
                        yield return new ModelResponseUsage
                        {
                            InputTokens = messageStart.Message.Usage.InputTokens,
                            OutputTokens = 0
                        };
                    }
                    break;

                case RawMessageStopEvent:
                    yield return new ModelResponseStreamEnd
                    {
                        Reason = InternalStopReason.EndTurn,
                        Metadata = new Dictionary<string, object>
                        {
                            ["provider"] = "anthropic"
                        }
                    };
                    break;
            }
        }
    }

    private static (string? systemPrompt, MessageParam[] messages) MapMessages(
        IReadOnlyList<InternalMessage> messages)
    {
        string? systemPrompt = null;
        var result = new List<MessageParam>();

        foreach (var msg in messages)
        {
            if (msg is not InternalUserMessage userMsg) continue;

            switch (msg.Role)
            {
                case InternalMessageRole.System:
                    systemPrompt = userMsg.Content;
                    break;
                case InternalMessageRole.User:
                    result.Add(new MessageParam { Role = Role.User, Content = userMsg.Content });
                    break;
                case InternalMessageRole.Assistant:
                    result.Add(new MessageParam { Role = Role.Assistant, Content = userMsg.Content });
                    break;
            }
        }

        return (systemPrompt, result.ToArray());
    }
}
