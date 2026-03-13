using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Anthropic;
using Anthropic.Models.Beta.Messages;
using DonkeyWork.Agents.Actors.Contracts.Messages;
using DonkeyWork.Agents.Actors.Core.Providers.Responses;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Actors.Core.Providers.Anthropic;

internal sealed class AnthropicProvider : IAiProvider
{
    private readonly ILogger<AnthropicProvider> _logger;
    private readonly HttpClient _httpClient;

    public AnthropicProvider(ILogger<AnthropicProvider> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient(nameof(AnthropicProvider));
        _httpClient.Timeout = TimeSpan.FromMinutes(10);
    }

    public IAsyncEnumerable<ModelResponseBase> StreamCompletionAsync(
        string systemPrompt,
        IReadOnlyList<InternalMessage> messages,
        IReadOnlyList<InternalToolDefinition>? tools,
        ProviderOptions options,
        CancellationToken ct = default)
    {
        _httpClient.DefaultRequestHeaders.Add("anthropic-beta", "interleaved-thinking-2025-05-14,web-fetch-2025-09-10");
        var client = new AnthropicClient { ApiKey = options.ApiKey, HttpClient = _httpClient };
        var messageParams = MapMessages(messages);
        var mappedTools = AnthropicToolMapper.MapTools(tools, options);

        // Compute thinking, effort, and temperature before the initializer (init-only props)
        BetaThinkingConfigParam? thinking = null;
        BetaOutputConfig? outputConfig = null;
        double? temperature = null;

        if (options.ReasoningEffort is not null)
        {
            // Enum-based reasoning effort (from agent builder)
            if (options.ReasoningEffort == Common.Contracts.Enums.ReasoningEffort.None)
            {
                thinking = new BetaThinkingConfigDisabled();
            }
            else
            {
                thinking = new BetaThinkingConfigAdaptive();
                temperature = 1; // Required by Anthropic when thinking is enabled
                outputConfig = new BetaOutputConfig
                {
                    Effort = options.ReasoningEffort.Value switch
                    {
                        Common.Contracts.Enums.ReasoningEffort.Low => Effort.Low,
                        Common.Contracts.Enums.ReasoningEffort.Medium => Effort.Medium,
                        Common.Contracts.Enums.ReasoningEffort.High => Effort.High,
                        _ => Effort.Medium,
                    }
                };
            }
        }
        else if (options.ThinkingBudgetTokens is > 0)
        {
            // Budget-based thinking (from hardcoded swarm contracts)
            thinking = new BetaThinkingConfigEnabled
            {
                BudgetTokens = options.ThinkingBudgetTokens.Value
            };
            temperature = 1; // Required by Anthropic when thinking is enabled
        }
        else if (options.ThinkingBudgetTokens is 0)
        {
            thinking = new BetaThinkingConfigDisabled();
        }

        var parameters = new MessageCreateParams
        {
            Model = options.ModelId,
            MaxTokens = options.MaxTokens,
            Messages = messageParams,
            System = systemPrompt,
            Temperature = temperature,
            Tools = mappedTools!,
            Thinking = thinking!,
            OutputConfig = outputConfig!,
        };

        return options.Stream
            ? StreamCore(client, parameters, ct)
            : CompleteCore(client, parameters, ct);
    }

    /// <summary>
    /// Non-streaming: single API call, yields complete content blocks.
    /// </summary>
    private async IAsyncEnumerable<ModelResponseBase> CompleteCore(
        AnthropicClient client,
        MessageCreateParams parameters,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var message = await client.Beta.Messages.Create(parameters, ct);

        // Usage
        if (message.Usage is not null)
        {
            yield return new ModelResponseUsage
            {
                InputTokens = (int)message.Usage.InputTokens,
                OutputTokens = (int)message.Usage.OutputTokens,
                WebSearchRequests = (int)(message.Usage.ServerToolUse?.WebSearchRequests ?? 0)
            };
        }

        // Content blocks — each yielded as a complete item
        int blockIndex = 0;
        foreach (var block in message.Content)
        {
            if (block.TryPickText(out var textBlock))
            {
                yield return new ModelResponseTextContent
                {
                    BlockIndex = blockIndex,
                    Content = textBlock.Text
                };

                // Citations embedded in the text block
                if (textBlock.Citations is not null)
                {
                    foreach (var citation in textBlock.Citations)
                    {
                        if (citation.TryPickCitationsWebSearchResultLocation(out var webCitation))
                        {
                            yield return new ModelResponseCitationContent
                            {
                                Index = blockIndex,
                                Title = webCitation.Title ?? "",
                                Url = webCitation.Url ?? "",
                                CitedText = webCitation.CitedText ?? ""
                            };
                        }
                    }
                }
            }
            else if (block.TryPickToolUse(out var toolUse))
            {
                var inputJson = JsonSerializer.Serialize(toolUse.Input);
                yield return new ModelResponseToolCall
                {
                    BlockIndex = blockIndex,
                    ToolName = toolUse.Name,
                    ToolUseId = toolUse.ID,
                    Input = JsonSerializer.Deserialize<JsonElement>(inputJson)
                };
            }
            else if (block.TryPickThinking(out var thinkingBlock))
            {
                yield return new ModelResponseThinkingContent
                {
                    Index = blockIndex,
                    Content = thinkingBlock.Thinking ?? "",
                    Signature = thinkingBlock.Signature ?? ""
                };
            }
            else if (block.TryPickServerToolUse(out var serverTool))
            {
                JsonElement? serverInput = null;
                try { serverInput = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(serverTool.Input)); }
                catch { /* ignore parse failures */ }

                yield return new ModelResponseServerToolUse
                {
                    BlockIndex = blockIndex,
                    ToolUseId = serverTool.ID,
                    ToolName = serverTool.Name,
                    Input = serverInput
                };
            }
            else if (block.TryPickWebSearchToolResult(out var searchResult))
            {
                yield return new ModelResponseWebSearchResult
                {
                    BlockIndex = blockIndex,
                    ToolUseId = searchResult.ToolUseID,
                    RawJson = JsonSerializer.Serialize(searchResult)
                };
            }
            else if (block.TryPickWebFetchToolResult(out var fetchResult))
            {
                yield return new ModelResponseWebFetchResult
                {
                    BlockIndex = blockIndex,
                    ToolUseId = fetchResult.ToolUseID,
                    RawJson = JsonSerializer.Serialize(fetchResult)
                };
            }
            else if (block.TryPickToolSearchToolResult(out var toolSearchResult))
            {
                yield return new ModelResponseToolSearchResult
                {
                    BlockIndex = blockIndex,
                    ToolUseId = toolSearchResult.ToolUseID,
                    RawJson = JsonSerializer.Serialize(toolSearchResult)
                };
            }

            blockIndex++;
        }

        // Metadata
        var stopReason = ParseStopReason(message.StopReason?.ToString() ?? "end_turn");
        yield return new ModelResponseMetadata
        {
            StopReason = stopReason,
            Properties = new Dictionary<string, object> { ["provider"] = "anthropic" }
        };
    }

    /// <summary>
    /// Streaming: yields deltas as they arrive from the SSE stream.
    /// </summary>
    private async IAsyncEnumerable<ModelResponseBase> StreamCore(
        AnthropicClient client,
        MessageCreateParams parameters,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        // Track content blocks for accumulating tool input JSON
        var toolInputBuffers = new Dictionary<long, StringBuilder>();
        var toolBlockInfo = new Dictionary<long, (string Id, string Name)>();
        // Track server tool blocks (web search, web fetch)
        var serverToolBlockInfo = new Dictionary<long, (string Id, string Name)>();
        var serverToolInputBuffers = new Dictionary<long, StringBuilder>();
        InternalStopReason stopReason = InternalStopReason.EndTurn;

        await foreach (var streamEvent in client.Beta.Messages.CreateStreaming(parameters, ct)
            .WithCancellation(ct))
        {
            if (streamEvent is null) continue;

            if (streamEvent.TryPickStart(out var messageStart))
            {
                _logger.LogInformation(
                    "Stream message_start: Message={HasMessage}, Usage={HasUsage}, InputTokens={InputTokens}",
                    messageStart.Message is not null,
                    messageStart.Message?.Usage is not null,
                    (int)(messageStart.Message?.Usage?.InputTokens ?? 0));

                if (messageStart.Message?.Usage is not null)
                {
                    var usage = messageStart.Message.Usage;
                    yield return new ModelResponseUsage
                    {
                        InputTokens = (int)usage.InputTokens,
                        OutputTokens = 0,
                        WebSearchRequests = (int)(usage.ServerToolUse?.WebSearchRequests ?? 0)
                    };
                }
            }
            else if (streamEvent.TryPickContentBlockStart(out var blockStart))
            {
                var index = (long)blockStart.Index;

                if (blockStart.ContentBlock.TryPickBetaToolUse(out var toolUseStart))
                {
                    toolInputBuffers[index] = new StringBuilder();
                    toolBlockInfo[index] = (toolUseStart.ID, toolUseStart.Name);
                }
                else if (blockStart.ContentBlock.TryPickBetaServerToolUse(out var serverToolStart))
                {
                    serverToolInputBuffers[index] = new StringBuilder();
                    serverToolBlockInfo[index] = (serverToolStart.ID, serverToolStart.Name);

                    yield return new ModelResponseServerToolUse
                    {
                        BlockIndex = (int)index,
                        ToolUseId = serverToolStart.ID,
                        ToolName = serverToolStart.Name
                    };
                }
                else if (blockStart.ContentBlock.TryPickBetaWebSearchToolResult(out var searchResult))
                {
                    var rawJson = JsonSerializer.Serialize(searchResult);
                    yield return new ModelResponseWebSearchResult
                    {
                        BlockIndex = (int)index,
                        ToolUseId = searchResult.ToolUseID,
                        RawJson = rawJson
                    };
                }
                else if (blockStart.ContentBlock.TryPickBetaWebFetchToolResult(out var fetchResult))
                {
                    var rawJson = JsonSerializer.Serialize(fetchResult);
                    yield return new ModelResponseWebFetchResult
                    {
                        BlockIndex = (int)index,
                        ToolUseId = fetchResult.ToolUseID,
                        RawJson = rawJson
                    };
                }
                else if (blockStart.ContentBlock.TryPickBetaToolSearchToolResult(out var toolSearchResult))
                {
                    var rawJson = JsonSerializer.Serialize(toolSearchResult);
                    yield return new ModelResponseToolSearchResult
                    {
                        BlockIndex = (int)index,
                        ToolUseId = toolSearchResult.ToolUseID,
                        RawJson = rawJson
                    };
                }
                else if (blockStart.ContentBlock.TryPickBetaThinking(out var thinkingStart))
                {
                    yield return new ModelResponseThinkingContent
                    {
                        Index = (int)index,
                        Content = thinkingStart.Thinking ?? ""
                    };
                }
            }
            else if (streamEvent.TryPickContentBlockDelta(out var blockDelta))
            {
                var index = (long)blockDelta.Index;

                if (blockDelta.Delta.TryPickText(out var textDelta) && !string.IsNullOrEmpty(textDelta.Text))
                {
                    yield return new ModelResponseTextContent
                    {
                        BlockIndex = (int)index,
                        Content = textDelta.Text
                    };
                }
                else if (blockDelta.Delta.TryPickInputJson(out var inputJson))
                {
                    // Could be client tool or server tool input JSON
                    if (toolInputBuffers.TryGetValue(index, out var buffer))
                    {
                        buffer.Append(inputJson.PartialJson);
                    }
                    else if (serverToolInputBuffers.TryGetValue(index, out var serverBuffer))
                    {
                        serverBuffer.Append(inputJson.PartialJson);
                    }
                }
                else if (blockDelta.Delta.TryPickCitations(out var citationsDelta))
                {
                    // Extract citation info from the delta
                    if (citationsDelta.Citation.TryPickBetaCitationsWebSearchResultLocation(
                            out var webCitation))
                    {
                        yield return new ModelResponseCitationContent
                        {
                            Index = (int)index,
                            Title = webCitation.Title ?? "",
                            Url = webCitation.Url ?? "",
                            CitedText = webCitation.CitedText ?? ""
                        };
                    }
                }
                else if (blockDelta.Delta.TryPickThinking(out var thinkingDelta))
                {
                    yield return new ModelResponseThinkingContent
                    {
                        Index = (int)index,
                        Content = thinkingDelta.Thinking ?? ""
                    };
                }
                else if (blockDelta.Delta.TryPickSignature(out var signatureDelta))
                {
                    yield return new ModelResponseThinkingContent
                    {
                        Index = (int)index,
                        Content = "",
                        Signature = signatureDelta.Signature ?? ""
                    };
                }
            }
            else if (streamEvent.TryPickContentBlockStop(out var blockStop))
            {
                var index = (long)blockStop.Index;

                // Finalize client tool calls
                if (toolInputBuffers.TryGetValue(index, out var inputBuffer) &&
                    toolBlockInfo.TryGetValue(index, out var info))
                {
                    var jsonStr = inputBuffer.Length == 0 ? "{}" : inputBuffer.ToString();
                    JsonElement inputElement;
                    try
                    {
                        inputElement = JsonSerializer.Deserialize<JsonElement>(jsonStr);
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse tool input JSON for {ToolName}", info.Name);
                        inputElement = JsonSerializer.Deserialize<JsonElement>("{}");
                    }

                    yield return new ModelResponseToolCall
                    {
                        BlockIndex = (int)index,
                        ToolName = info.Name,
                        ToolUseId = info.Id,
                        Input = inputElement
                    };

                    toolInputBuffers.Remove(index);
                    toolBlockInfo.Remove(index);
                }

                // Finalize server tool input (query for web search)
                if (serverToolInputBuffers.TryGetValue(index, out var serverInputBuffer) &&
                    serverToolBlockInfo.TryGetValue(index, out var serverInfo))
                {
                    var serverJsonStr = serverInputBuffer.Length == 0 ? "{}" : serverInputBuffer.ToString();
                    JsonElement serverInputElement;
                    try { serverInputElement = JsonSerializer.Deserialize<JsonElement>(serverJsonStr); }
                    catch { serverInputElement = JsonSerializer.Deserialize<JsonElement>("{}"); }

                    // Re-emit with input populated so grains can extract the query
                    yield return new ModelResponseServerToolUse
                    {
                        BlockIndex = (int)index,
                        ToolUseId = serverInfo.Id,
                        ToolName = serverInfo.Name,
                        Input = serverInputElement
                    };
                }

                serverToolInputBuffers.Remove(index);
                serverToolBlockInfo.Remove(index);
            }
            else if (streamEvent.TryPickDelta(out var messageDelta))
            {
                if (messageDelta.Usage is not null)
                {
                    yield return new ModelResponseUsage
                    {
                        InputTokens = 0,
                        OutputTokens = (int)messageDelta.Usage.OutputTokens,
                        WebSearchRequests = (int)(messageDelta.Usage.ServerToolUse?.WebSearchRequests ?? 0)
                    };
                }

                if (messageDelta.Delta?.StopReason is not null)
                {
                    var reason = messageDelta.Delta.StopReason.ToString() ?? "";
                    stopReason = ParseStopReason(reason);
                }
            }
        }

        yield return new ModelResponseMetadata
        {
            StopReason = stopReason,
            Properties = new Dictionary<string, object> { ["provider"] = "anthropic" }
        };
    }

    private static InternalStopReason ParseStopReason(string reason)
    {
        if (reason.Contains("tool_use", StringComparison.OrdinalIgnoreCase))
            return InternalStopReason.ToolUse;
        if (reason.Contains("max_tokens", StringComparison.OrdinalIgnoreCase))
            return InternalStopReason.MaxTokens;
        if (reason.Contains("end_turn", StringComparison.OrdinalIgnoreCase))
            return InternalStopReason.EndTurn;
        return InternalStopReason.EndTurn;
    }

    internal static BetaMessageParam[] MapMessages(IReadOnlyList<InternalMessage> messages)
    {
        var result = new List<BetaMessageParam>();

        foreach (var msg in messages)
        {
            switch (msg)
            {
                case InternalContentMessage { Role: InternalMessageRole.User } userMsg:
                    result.Add(new BetaMessageParam { Role = Role.User, Content = userMsg.Content });
                    break;

                case InternalContentMessage { Role: InternalMessageRole.Assistant } asstMsg:
                    result.Add(new BetaMessageParam { Role = Role.Assistant, Content = asstMsg.Content });
                    break;

                case InternalAssistantMessage asstToolMsg:
                    var blocks = MapAssistantContentBlocks(asstToolMsg);
                    result.Add(new BetaMessageParam
                    {
                        Role = Role.Assistant,
                        Content = blocks
                    });
                    break;

                case InternalToolResultMessage toolResult:
                    var toolResultBlock = new BetaToolResultBlockParam
                    {
                        ToolUseID = toolResult.ToolUseId,
                        Content = toolResult.Content,
                        IsError = toolResult.IsError
                    };
                    result.Add(new BetaMessageParam
                    {
                        Role = Role.User,
                        Content = new List<BetaContentBlockParam> { toolResultBlock }
                    });
                    break;

                // System messages are handled via the systemPrompt parameter, skip here
                case InternalContentMessage { Role: InternalMessageRole.System }:
                    break;
            }
        }

        return result.ToArray();
    }

    private static List<BetaContentBlockParam> MapAssistantContentBlocks(InternalAssistantMessage msg)
    {
        // If ContentBlocks is populated, use it for full-fidelity round-tripping
        if (msg.ContentBlocks.Count > 0)
        {
            var blocks = new List<BetaContentBlockParam>();
            foreach (var block in msg.ContentBlocks)
            {
                switch (block)
                {
                    case InternalTextBlock textBlock:
                        blocks.Add(new BetaTextBlockParam { Text = textBlock.Text });
                        break;

                    case InternalToolUseBlock toolUse:
                        var inputDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
                            toolUse.Input.GetRawText()) ?? new Dictionary<string, JsonElement>();
                        blocks.Add(new BetaToolUseBlockParam
                        {
                            ID = toolUse.Id,
                            Name = toolUse.Name,
                            Input = inputDict
                        });
                        break;

                    case InternalServerToolUseBlock serverToolUse:
                        var serverInputDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
                            serverToolUse.Input.GetRawText()) ?? new Dictionary<string, JsonElement>();
                        blocks.Add(new BetaServerToolUseBlockParam
                        {
                            ID = serverToolUse.Id,
                            Name = serverToolUse.Name,
                            Input = serverInputDict
                        });
                        break;

                    case InternalWebSearchResultBlock searchResult:
                        var searchResultDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
                            searchResult.RawJson) ?? new Dictionary<string, JsonElement>();
                        blocks.Add(BetaWebSearchToolResultBlockParam.FromRawUnchecked(searchResultDict));
                        break;

                    case InternalWebFetchToolResultBlock fetchResult:
                        var fetchResultDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
                            fetchResult.RawJson) ?? new Dictionary<string, JsonElement>();
                        blocks.Add(BetaWebFetchToolResultBlockParam.FromRawUnchecked(fetchResultDict));
                        break;

                    case InternalThinkingBlock thinkingBlock:
                        blocks.Add(new BetaThinkingBlockParam
                        {
                            Thinking = thinkingBlock.Text,
                            Signature = thinkingBlock.Signature ?? ""
                        });
                        break;

                    case InternalToolSearchResultBlock toolSearchResult:
                        var toolSearchDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
                            toolSearchResult.RawJson) ?? new Dictionary<string, JsonElement>();
                        blocks.Add(BetaToolSearchToolResultBlockParam.FromRawUnchecked(toolSearchDict));
                        break;

                    case InternalCitationBlock:
                        // Citations are output-only metadata; not round-tripped to the API
                        break;
                }
            }
            return blocks;
        }

        // Fallback: use TextContent + ToolUses (backward compat)
        var fallbackBlocks = new List<BetaContentBlockParam>();
        if (!string.IsNullOrEmpty(msg.TextContent))
        {
            fallbackBlocks.Add(new BetaTextBlockParam { Text = msg.TextContent });
        }
        foreach (var tu in msg.ToolUses)
        {
            var inputDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(tu.Input.GetRawText())
                ?? new Dictionary<string, JsonElement>();
            fallbackBlocks.Add(new BetaToolUseBlockParam
            {
                ID = tu.Id,
                Name = tu.Name,
                Input = inputDict
            });
        }
        return fallbackBlocks;
    }
}
