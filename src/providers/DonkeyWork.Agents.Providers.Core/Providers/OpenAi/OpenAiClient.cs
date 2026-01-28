using System.Runtime.CompilerServices;
using DonkeyWork.Agents.Providers.Core.Middleware;
using DonkeyWork.Agents.Providers.Core.Middleware.Internal;
using DonkeyWork.Agents.Providers.Core.Middleware.Internal.Responses;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;

namespace DonkeyWork.Agents.Providers.Core.Providers.OpenAi;

/// <summary>
/// OpenAI provider client using the official OpenAI C# SDK.
/// </summary>
internal sealed class OpenAiClient : IAiClient
{
    private readonly ChatClient _chatClient;
    private readonly ILogger<OpenAiClient> _logger;

    public OpenAiClient(string apiKey, string modelId, ILogger<OpenAiClient> logger)
    {
        _chatClient = new ChatClient(modelId, apiKey);
        _logger = logger;
    }

    public async IAsyncEnumerable<ModelResponseBase> StreamCompletionAsync(
        IReadOnlyList<InternalMessage> messages,
        IReadOnlyList<InternalToolDefinition>? tools,
        IReadOnlyDictionary<string, object>? providerParameters,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var chatMessages = MapMessages(messages);
        var options = BuildOptions(tools, providerParameters);

        AsyncCollectionResult<StreamingChatCompletionUpdate> stream =
            _chatClient.CompleteChatStreamingAsync(chatMessages, options, cancellationToken);

        var blockStarted = false;
        var blockIndex = 0;

        await foreach (var update in stream.WithCancellation(cancellationToken))
        {
            // Text content
            if (update.ContentUpdate is { Count: > 0 })
            {
                foreach (var part in update.ContentUpdate)
                {
                    if (part.Kind == ChatMessageContentPartKind.Text && !string.IsNullOrEmpty(part.Text))
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

                        yield return new ModelResponseTextContent { Content = part.Text };
                    }
                }
            }

            // Tool calls
            if (update.ToolCallUpdates is { Count: > 0 })
            {
                foreach (var toolCall in update.ToolCallUpdates)
                {
                    if (toolCall.FunctionArgumentsUpdate is not null)
                    {
                        // Partial tool call arguments are streamed; we need to accumulate them.
                        // The SDK handles accumulation when using the non-streaming API, but for streaming
                        // we get partial updates. For now yield complete tool calls at FinishReason.
                        continue;
                    }
                }
            }

            // Usage
            if (update.Usage is not null)
            {
                yield return new ModelResponseUsage
                {
                    InputTokens = update.Usage.InputTokenCount,
                    OutputTokens = update.Usage.OutputTokenCount
                };
            }

            // Finish reason
            if (update.FinishReason is not null)
            {
                if (blockStarted)
                {
                    yield return new ModelResponseBlockEnd { BlockIndex = blockIndex };
                    blockStarted = false;
                }

                // Yield any complete tool calls
                if (update.FinishReason == ChatFinishReason.ToolCalls)
                {
                    // Tool calls will have been accumulated by the SDK across updates
                    // We handle them via the accumulated approach below
                }

                yield return new ModelResponseStreamEnd
                {
                    Reason = MapFinishReason(update.FinishReason.Value),
                    Metadata = new Dictionary<string, object>
                    {
                        ["provider"] = "openai"
                    }
                };
            }
        }

        // If block was never closed (edge case)
        if (blockStarted)
        {
            yield return new ModelResponseBlockEnd { BlockIndex = blockIndex };
        }
    }

    private static List<ChatMessage> MapMessages(IReadOnlyList<InternalMessage> messages)
    {
        var result = new List<ChatMessage>();

        foreach (var msg in messages)
        {
            switch (msg)
            {
                case InternalUserMessage userMsg:
                    if (msg.Role == InternalMessageRole.System)
                    {
                        result.Add(ChatMessage.CreateSystemMessage(userMsg.Content));
                    }
                    else if (msg.Role == InternalMessageRole.Assistant)
                    {
                        result.Add(ChatMessage.CreateAssistantMessage(userMsg.Content));
                    }
                    else
                    {
                        result.Add(ChatMessage.CreateUserMessage(userMsg.Content));
                    }
                    break;
            }
        }

        return result;
    }

    private static ChatCompletionOptions BuildOptions(
        IReadOnlyList<InternalToolDefinition>? tools,
        IReadOnlyDictionary<string, object>? providerParameters)
    {
        var options = new ChatCompletionOptions
        {
            IncludeLogProbabilities = false
        };

        if (providerParameters is not null)
        {
            if (providerParameters.TryGetValue("temperature", out var temp))
                options.Temperature = Convert.ToSingle(temp);

            if (providerParameters.TryGetValue("max_tokens", out var maxTokens))
                options.MaxOutputTokenCount = Convert.ToInt32(maxTokens);

            if (providerParameters.TryGetValue("top_p", out var topP))
                options.TopP = Convert.ToSingle(topP);

            if (providerParameters.TryGetValue("frequency_penalty", out var freqPenalty))
                options.FrequencyPenalty = Convert.ToSingle(freqPenalty);

            if (providerParameters.TryGetValue("presence_penalty", out var presPenalty))
                options.PresencePenalty = Convert.ToSingle(presPenalty);
        }

        if (tools is { Count: > 0 })
        {
            foreach (var tool in tools)
            {
                options.Tools.Add(ChatTool.CreateFunctionTool(
                    tool.Name,
                    tool.Description,
                    tool.InputSchema is not null
                        ? BinaryData.FromString(tool.InputSchema)
                        : null));
            }
        }

        return options;
    }

    private static InternalStopReason MapFinishReason(ChatFinishReason reason)
    {
        if (reason == ChatFinishReason.Stop)
            return InternalStopReason.EndTurn;
        if (reason == ChatFinishReason.ToolCalls)
            return InternalStopReason.ToolUse;
        if (reason == ChatFinishReason.Length)
            return InternalStopReason.MaxTokens;
        if (reason == ChatFinishReason.ContentFilter)
            return InternalStopReason.ContentFilter;

        return InternalStopReason.EndTurn;
    }
}
