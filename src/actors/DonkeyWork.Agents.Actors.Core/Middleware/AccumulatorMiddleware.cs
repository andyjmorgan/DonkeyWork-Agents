using System.Text;
using System.Text.Json;
using DonkeyWork.Agents.Actors.Contracts.Messages;
using DonkeyWork.Agents.Actors.Core.Middleware.Messages;
using DonkeyWork.Agents.Actors.Core.Providers.Responses;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Actors.Core.Middleware;

internal sealed class AccumulatorMiddleware : IModelMiddleware
{
    private readonly ILogger<AccumulatorMiddleware> _logger;

    public AccumulatorMiddleware(ILogger<AccumulatorMiddleware> logger) => _logger = logger;

    public async IAsyncEnumerable<BaseMiddlewareMessage> ExecuteAsync(
        ModelMiddlewareContext context,
        Func<ModelMiddlewareContext, IAsyncEnumerable<BaseMiddlewareMessage>> next)
    {
        _logger.LogDebug("AccumulatorMiddleware entering");

        var textByBlock = new Dictionary<int, StringBuilder>();
        var toolCalls = new List<ToolUseRecord>();
        var thinkingByIndex = new Dictionary<int, ThinkingAccumulator>();

        // Ordered content blocks for faithful round-tripping
        var orderedBlocks = new List<(int Index, InternalContentBlock? Block)>();
        // Track which orderedBlocks entry corresponds to text blocks (need finalization)
        var textBlockPositions = new Dictionary<int, int>(); // blockIndex -> orderedBlocks position
        // Track which orderedBlocks entry corresponds to thinking blocks
        var thinkingBlockPositions = new Dictionary<int, int>();
        // Track server tool blocks by BlockIndex to avoid duplicates
        // (streaming emits two ModelResponseServerToolUse per block: at start and stop)
        var serverToolBlockPositions = new Dictionary<int, int>();

        await foreach (var msg in next(context))
        {
            // Pass through every message for real-time streaming
            yield return msg;

            // Simultaneously accumulate
            if (msg is ModelMiddlewareMessage { ModelMessage: var modelMsg })
            {
                switch (modelMsg)
                {
                    case ModelResponseTextContent text:
                        if (!textByBlock.TryGetValue(text.BlockIndex, out var sb))
                        {
                            sb = new StringBuilder();
                            textByBlock[text.BlockIndex] = sb;
                            // Reserve a slot in orderedBlocks (placeholder)
                            textBlockPositions[text.BlockIndex] = orderedBlocks.Count;
                            orderedBlocks.Add((text.BlockIndex, null));
                        }
                        sb.Append(text.Content);
                        break;

                    case ModelResponseToolCall toolCall:
                        toolCalls.Add(new ToolUseRecord(toolCall.ToolUseId, toolCall.ToolName, toolCall.Input));
                        orderedBlocks.Add((toolCall.BlockIndex,
                            new InternalToolUseBlock(toolCall.ToolUseId, toolCall.ToolName, toolCall.Input)));
                        break;

                    case ModelResponseServerToolUse serverTool:
                        // Streaming emits two ModelResponseServerToolUse per block:
                        // one at content_block_start (no input) and one at content_block_stop
                        // (with input). Deduplicate by BlockIndex — update in-place on second hit.
                        var serverInput = serverTool.Input ?? JsonSerializer.Deserialize<JsonElement>("{}");
                        var serverBlock = new InternalServerToolUseBlock(
                            serverTool.ToolUseId, serverTool.ToolName, serverInput);

                        if (serverToolBlockPositions.TryGetValue(serverTool.BlockIndex, out var existingPos))
                        {
                            // Second emission (block stop) — update with populated input
                            orderedBlocks[existingPos] = (serverTool.BlockIndex, serverBlock);
                        }
                        else
                        {
                            // First emission (block start)
                            serverToolBlockPositions[serverTool.BlockIndex] = orderedBlocks.Count;
                            orderedBlocks.Add((serverTool.BlockIndex, serverBlock));
                        }
                        break;

                    case ModelResponseWebSearchResult searchResult:
                        orderedBlocks.Add((searchResult.BlockIndex,
                            new InternalWebSearchResultBlock(searchResult.ToolUseId, searchResult.RawJson)));
                        break;

                    case ModelResponseWebFetchResult fetchResult:
                        orderedBlocks.Add((fetchResult.BlockIndex,
                            new InternalWebFetchToolResultBlock(fetchResult.ToolUseId, fetchResult.RawJson)));
                        break;

                    case ModelResponseThinkingContent thinking:
                        if (!thinkingByIndex.TryGetValue(thinking.Index, out var acc))
                        {
                            acc = new ThinkingAccumulator();
                            thinkingByIndex[thinking.Index] = acc;
                            thinkingBlockPositions[thinking.Index] = orderedBlocks.Count;
                            orderedBlocks.Add((thinking.Index, null));
                        }
                        if (!string.IsNullOrEmpty(thinking.Content))
                            acc.Text.Append(thinking.Content);
                        if (thinking.Signature is not null)
                            acc.Signature = thinking.Signature;
                        break;

                    case ModelResponseCitationContent citation:
                        orderedBlocks.Add((-1, new InternalCitationBlock(citation.Title, citation.Url, citation.CitedText)));
                        break;
                }
            }
        }

        // Finalize text block placeholders
        foreach (var (blockIndex, position) in textBlockPositions)
        {
            if (textByBlock.TryGetValue(blockIndex, out var textSb))
            {
                orderedBlocks[position] = (blockIndex, new InternalTextBlock(textSb.ToString()));
            }
        }

        // Finalize thinking block placeholders
        foreach (var (thinkIndex, position) in thinkingBlockPositions)
        {
            if (thinkingByIndex.TryGetValue(thinkIndex, out var thinkAcc))
            {
                orderedBlocks[position] = (thinkIndex,
                    new InternalThinkingBlock(thinkAcc.Text.ToString(), thinkAcc.Signature));
            }
        }

        // Build the accumulated assistant message
        var fullText = string.Join("", textByBlock.OrderBy(kv => kv.Key).Select(kv => kv.Value.ToString()));

        var contentBlocks = orderedBlocks
            .Where(b => b.Block is not null)
            .Select(b => b.Block!)
            .ToList();

        var assistantMessage = new InternalAssistantMessage
        {
            Role = InternalMessageRole.Assistant,
            TextContent = string.IsNullOrEmpty(fullText) ? null : fullText,
            ToolUses = toolCalls,
            ContentBlocks = contentBlocks
        };

        context.Messages.Add(assistantMessage);

        _logger.LogDebug(
            "AccumulatorMiddleware accumulated {BlockCount} content blocks, {ToolCount} tool uses, text={HasText}",
            contentBlocks.Count, toolCalls.Count, !string.IsNullOrEmpty(fullText));
    }

    private sealed class ThinkingAccumulator
    {
        public StringBuilder Text { get; } = new();
        public string? Signature { get; set; }
    }
}
