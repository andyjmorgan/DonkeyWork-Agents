#pragma warning disable OPENAI001 // Experimental API

using System.Runtime.CompilerServices;
using System.Text;
using DonkeyWork.Agents.Providers.Contracts.Models.Pipeline;
using DonkeyWork.Agents.Providers.Core.Middleware.Internal;
using DonkeyWork.Agents.Providers.Core.Middleware.Internal.Responses;
using Microsoft.Extensions.Logging;
using OpenAI.Responses;

namespace DonkeyWork.Agents.Providers.Core.Providers.OpenAi;

/// <summary>
/// OpenAI provider client using the Responses API (recommended approach).
/// </summary>
internal sealed class OpenAiResponsesClient : IAiClient
{
    private readonly ResponsesClient _responsesClient;
    private readonly ILogger<OpenAiResponsesClient> _logger;

    public OpenAiResponsesClient(string apiKey, string modelId, ILogger<OpenAiResponsesClient> logger)
    {
        _responsesClient = new ResponsesClient(modelId, apiKey);
        _logger = logger;
    }

    public async IAsyncEnumerable<ModelResponseBase> StreamCompletionAsync(
        IReadOnlyList<InternalMessage> messages,
        IReadOnlyList<InternalToolDefinition>? tools,
        IReadOnlyDictionary<string, object>? providerParameters,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var inputItems = MapMessages(messages);
        var options = BuildOptions(inputItems, tools, providerParameters, streaming: true);

        var stream = _responsesClient.CreateResponseStreamingAsync(options, cancellationToken);

        var currentToolCallId = string.Empty;
        var currentToolName = string.Empty;
        var toolArgumentsBuilder = new StringBuilder();
        var hasFunctionCalls = false;

        await foreach (var update in stream.WithCancellation(cancellationToken))
        {
            switch (update)
            {
                // Content part added - indicates start of a new content block
                case StreamingResponseContentPartAddedUpdate contentPartAdded:
                    yield return new ModelResponseBlockStart
                    {
                        BlockIndex = contentPartAdded.ContentIndex,
                        Type = MapContentPartKind(contentPartAdded.Part?.Kind)
                    };
                    break;

                // Text delta - streaming text content
                case StreamingResponseOutputTextDeltaUpdate textDelta:
                    if (!string.IsNullOrEmpty(textDelta.Delta))
                    {
                        yield return new ModelResponseTextContent
                        {
                            BlockIndex = textDelta.ContentIndex,
                            Content = textDelta.Delta
                        };
                    }
                    break;

                // Content part done - end of a content block
                case StreamingResponseContentPartDoneUpdate contentPartDone:
                    yield return new ModelResponseBlockEnd { BlockIndex = contentPartDone.ContentIndex };
                    break;

                // Output item added - could be a function call starting
                case StreamingResponseOutputItemAddedUpdate outputItemAdded:
                    if (outputItemAdded.Item is FunctionCallResponseItem functionCall)
                    {
                        currentToolCallId = functionCall.CallId;
                        currentToolName = functionCall.FunctionName;
                        toolArgumentsBuilder.Clear();
                    }
                    break;

                // Function call arguments delta - streaming tool arguments
                case StreamingResponseFunctionCallArgumentsDeltaUpdate argsDelta:
                    if (argsDelta.Delta is not null && !argsDelta.Delta.ToMemory().IsEmpty)
                    {
                        toolArgumentsBuilder.Append(argsDelta.Delta.ToString());
                    }
                    break;

                // Function call arguments done - complete tool call
                case StreamingResponseFunctionCallArgumentsDoneUpdate argsDone:
                    if (!string.IsNullOrEmpty(currentToolCallId))
                    {
                        var arguments = argsDone.FunctionArguments?.ToString() ?? toolArgumentsBuilder.ToString();
                        yield return new ModelResponseToolCall
                        {
                            CallId = currentToolCallId,
                            ToolName = currentToolName,
                            Arguments = arguments
                        };
                        hasFunctionCalls = true;
                        currentToolCallId = string.Empty;
                        currentToolName = string.Empty;
                        toolArgumentsBuilder.Clear();
                    }
                    break;

                // Output item done - can also contain complete function calls
                case StreamingResponseOutputItemDoneUpdate outputItemDone:
                    if (outputItemDone.Item is FunctionCallResponseItem completedFunctionCall &&
                        !string.IsNullOrEmpty(completedFunctionCall.CallId))
                    {
                        // If we haven't already emitted this tool call via argsDone
                        if (string.IsNullOrEmpty(currentToolCallId) ||
                            currentToolCallId != completedFunctionCall.CallId)
                        {
                            yield return new ModelResponseToolCall
                            {
                                CallId = completedFunctionCall.CallId,
                                ToolName = completedFunctionCall.FunctionName,
                                Arguments = completedFunctionCall.FunctionArguments?.ToString() ?? "{}"
                            };
                            hasFunctionCalls = true;
                        }
                    }
                    break;

                // Response completed - final event with usage info
                case StreamingResponseCompletedUpdate completed:
                    if (completed.Response?.Usage is not null)
                    {
                        yield return new ModelResponseUsage
                        {
                            InputTokens = completed.Response.Usage.InputTokenCount,
                            OutputTokens = completed.Response.Usage.OutputTokenCount
                        };
                    }

                    // If there were function calls, report ToolUse reason
                    var stopReason = hasFunctionCalls
                        ? InternalStopReason.ToolUse
                        : MapStatus(completed.Response?.Status, completed.Response?.IncompleteStatusDetails);

                    yield return new ModelResponseStreamEnd
                    {
                        Reason = stopReason,
                        Metadata = new Dictionary<string, object>
                        {
                            ["provider"] = "openai",
                            ["api"] = "responses"
                        }
                    };
                    break;

                // Response incomplete - stopped early
                case StreamingResponseIncompleteUpdate incomplete:
                    if (incomplete.Response?.Usage is not null)
                    {
                        yield return new ModelResponseUsage
                        {
                            InputTokens = incomplete.Response.Usage.InputTokenCount,
                            OutputTokens = incomplete.Response.Usage.OutputTokenCount
                        };
                    }

                    yield return new ModelResponseStreamEnd
                    {
                        Reason = MapStatus(incomplete.Response?.Status, incomplete.Response?.IncompleteStatusDetails),
                        Metadata = new Dictionary<string, object>
                        {
                            ["provider"] = "openai",
                            ["api"] = "responses"
                        }
                    };
                    break;

                // Response failed
                case StreamingResponseFailedUpdate:
                    yield return new ModelResponseStreamEnd
                    {
                        Reason = InternalStopReason.Incomplete,
                        Metadata = new Dictionary<string, object>
                        {
                            ["provider"] = "openai",
                            ["api"] = "responses",
                            ["error"] = "Response failed"
                        }
                    };
                    break;
            }
        }
    }

    public async IAsyncEnumerable<ModelResponseBase> CompleteAsync(
        IReadOnlyList<InternalMessage> messages,
        IReadOnlyList<InternalToolDefinition>? tools,
        IReadOnlyDictionary<string, object>? providerParameters,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var inputItems = MapMessages(messages);
        var options = BuildOptions(inputItems, tools, providerParameters, streaming: false);

        var response = await _responsesClient.CreateResponseAsync(options, cancellationToken);
        var result = response.Value;

        var contentIndex = 0;
        var hasFunctionCalls = false;

        foreach (var outputItem in result.OutputItems)
        {
            if (outputItem is MessageResponseItem messageItem)
            {
                foreach (var contentPart in messageItem.Content)
                {
                    if (contentPart.Kind == ResponseContentPartKind.OutputText && !string.IsNullOrEmpty(contentPart.Text))
                    {
                        yield return new ModelResponseBlockStart
                        {
                            BlockIndex = contentIndex,
                            Type = InternalContentBlockType.Text
                        };

                        yield return new ModelResponseTextContent
                        {
                            BlockIndex = contentIndex,
                            Content = contentPart.Text
                        };

                        yield return new ModelResponseBlockEnd { BlockIndex = contentIndex };
                        contentIndex++;
                    }
                }
            }
            else if (outputItem is FunctionCallResponseItem functionCall)
            {
                yield return new ModelResponseToolCall
                {
                    CallId = functionCall.CallId,
                    ToolName = functionCall.FunctionName,
                    Arguments = functionCall.FunctionArguments?.ToString() ?? "{}"
                };
                hasFunctionCalls = true;
            }
        }

        if (result.Usage is not null)
        {
            yield return new ModelResponseUsage
            {
                InputTokens = result.Usage.InputTokenCount,
                OutputTokens = result.Usage.OutputTokenCount
            };
        }

        var stopReason = hasFunctionCalls
            ? InternalStopReason.ToolUse
            : MapStatus(result.Status, result.IncompleteStatusDetails);

        yield return new ModelResponseStreamEnd
        {
            Reason = stopReason,
            Metadata = new Dictionary<string, object>
            {
                ["provider"] = "openai",
                ["api"] = "responses"
            }
        };
    }

    private static List<ResponseItem> MapMessages(IReadOnlyList<InternalMessage> messages)
    {
        var result = new List<ResponseItem>();

        foreach (var msg in messages)
        {
            if (msg is not InternalContentMessage contentMsg) continue;

            switch (msg.Role)
            {
                case InternalMessageRole.System:
                    result.Add(ResponseItem.CreateSystemMessageItem(contentMsg.GetTextContent()));
                    break;

                case InternalMessageRole.User:
                    var contentParts = MapContentParts(contentMsg.Content);
                    if (contentParts.Count > 0)
                    {
                        result.Add(ResponseItem.CreateUserMessageItem(contentParts));
                    }
                    break;

                case InternalMessageRole.Assistant:
                    result.Add(ResponseItem.CreateAssistantMessageItem(contentMsg.GetTextContent()));
                    break;
            }
        }

        return result;
    }

    private static List<ResponseContentPart> MapContentParts(IReadOnlyList<ChatContentPart> content)
    {
        var parts = new List<ResponseContentPart>();

        foreach (var part in content)
        {
            switch (part)
            {
                case TextChatContentPart text:
                    parts.Add(ResponseContentPart.CreateInputTextPart(text.Text));
                    break;

                case ImageChatContentPart image:
                    if (image.SourceType == "base64")
                    {
                        var imageBytes = BinaryData.FromBytes(Convert.FromBase64String(image.Data));
                        parts.Add(ResponseContentPart.CreateInputImagePart(imageBytes, image.MediaType));
                    }
                    else
                    {
                        parts.Add(ResponseContentPart.CreateInputImagePart(new Uri(image.Data)));
                    }
                    break;
            }
        }

        return parts;
    }

    private static CreateResponseOptions BuildOptions(
        List<ResponseItem> inputItems,
        IReadOnlyList<InternalToolDefinition>? tools,
        IReadOnlyDictionary<string, object>? providerParameters,
        bool streaming)
    {
        var options = new CreateResponseOptions(inputItems)
        {
            StreamingEnabled = streaming,
            StoredOutputEnabled = false // Disable server-side conversation memory
        };

        if (providerParameters is not null)
        {
            if (providerParameters.TryGetValue("temperature", out var temp))
                options.Temperature = Convert.ToSingle(temp);

            if (providerParameters.TryGetValue("max_tokens", out var maxTokens))
                options.MaxOutputTokenCount = Convert.ToInt32(maxTokens);

            if (providerParameters.TryGetValue("top_p", out var topP))
                options.TopP = Convert.ToSingle(topP);
        }

        if (tools is { Count: > 0 })
        {
            foreach (var tool in tools)
            {
                var functionTool = ResponseTool.CreateFunctionTool(
                    functionName: tool.Name,
                    functionParameters: tool.InputSchema is not null
                        ? BinaryData.FromString(tool.InputSchema)
                        : null,
                    strictModeEnabled: false,
                    functionDescription: tool.Description);

                options.Tools.Add(functionTool);
            }
        }

        return options;
    }

    private static InternalContentBlockType MapContentPartKind(ResponseContentPartKind? kind)
    {
        return kind switch
        {
            ResponseContentPartKind.OutputText => InternalContentBlockType.Text,
            ResponseContentPartKind.Refusal => InternalContentBlockType.Text,
            _ => InternalContentBlockType.Text
        };
    }

    private static InternalStopReason MapStatus(ResponseStatus? status, ResponseIncompleteStatusDetails? incompleteDetails)
    {
        if (incompleteDetails?.Reason == ResponseIncompleteStatusReason.MaxOutputTokens)
            return InternalStopReason.MaxTokens;

        if (incompleteDetails?.Reason == ResponseIncompleteStatusReason.ContentFilter)
            return InternalStopReason.ContentFilter;

        return status switch
        {
            ResponseStatus.Completed => InternalStopReason.EndTurn,
            ResponseStatus.Incomplete => InternalStopReason.Incomplete,
            ResponseStatus.Cancelled => InternalStopReason.Cancelled,
            ResponseStatus.Failed => InternalStopReason.Incomplete,
            _ => InternalStopReason.EndTurn
        };
    }
}
