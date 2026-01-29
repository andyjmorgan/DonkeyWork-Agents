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

        var blockIndex = 0;
        var textBlockStarted = false;

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
                        BlockIndex = blockIndex,
                        Type = InternalContentBlockType.Text
                    };
                    textBlockStarted = true;
                }
            }
            // Content block delta (text streaming)
            else if (streamEvent.TryPickContentBlockDelta(out var blockDelta))
            {
                if (blockDelta.Delta.TryPickText(out var textDelta))
                {
                    yield return new ModelResponseTextContent { Content = textDelta.Text };
                }
            }
            // Content block stop
            else if (streamEvent.TryPickContentBlockStop(out _))
            {
                if (textBlockStarted)
                {
                    yield return new ModelResponseBlockEnd { BlockIndex = blockIndex };
                    blockIndex++;
                    textBlockStarted = false;
                }
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
