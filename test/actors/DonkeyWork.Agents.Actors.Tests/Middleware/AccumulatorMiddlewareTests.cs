using System.Text.Json;
using DonkeyWork.Agents.Actors.Contracts.Messages;
using DonkeyWork.Agents.Actors.Core.Middleware;
using DonkeyWork.Agents.Actors.Core.Middleware.Messages;
using DonkeyWork.Agents.Actors.Core.Providers;
using DonkeyWork.Agents.Actors.Core.Providers.Responses;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DonkeyWork.Agents.Actors.Tests.Middleware;

public class AccumulatorMiddlewareTests
{
    private readonly AccumulatorMiddleware _middleware;

    public AccumulatorMiddlewareTests()
    {
        _middleware = new AccumulatorMiddleware(NullLogger<AccumulatorMiddleware>.Instance);
    }

    #region Text Accumulation Tests

    [Fact]
    public async Task ExecuteAsync_WithTextContent_AccumulatesIntoAssistantMessage()
    {
        // Arrange
        var context = CreateContext();
        var messages = new BaseMiddlewareMessage[]
        {
            ModelMsg(new ModelResponseTextContent { Content = "Hello ", BlockIndex = 0 }),
            ModelMsg(new ModelResponseTextContent { Content = "World", BlockIndex = 0 }),
        };

        // Act
        await ConsumeAll(_middleware.ExecuteAsync(context, NextThatYields(messages)));

        // Assert
        var assistant = Assert.Single(context.Messages.OfType<InternalAssistantMessage>());
        Assert.Equal("Hello World", assistant.TextContent);
    }

    [Fact]
    public async Task ExecuteAsync_WithMultipleTextBlocks_AccumulatesSeparately()
    {
        // Arrange
        var context = CreateContext();
        var messages = new BaseMiddlewareMessage[]
        {
            ModelMsg(new ModelResponseTextContent { Content = "Block 0", BlockIndex = 0 }),
            ModelMsg(new ModelResponseTextContent { Content = "Block 1", BlockIndex = 1 }),
        };

        // Act
        await ConsumeAll(_middleware.ExecuteAsync(context, NextThatYields(messages)));

        // Assert
        var assistant = Assert.Single(context.Messages.OfType<InternalAssistantMessage>());
        Assert.Equal("Block 0Block 1", assistant.TextContent);
        Assert.Equal(2, assistant.ContentBlocks.Count);
        Assert.All(assistant.ContentBlocks, b => Assert.IsType<InternalTextBlock>(b));
    }

    [Fact]
    public async Task ExecuteAsync_PassesThroughAllMessages()
    {
        // Arrange
        var context = CreateContext();
        var msg = ModelMsg(new ModelResponseTextContent { Content = "Hello", BlockIndex = 0 });

        // Act
        var output = new List<BaseMiddlewareMessage>();
        await foreach (var m in _middleware.ExecuteAsync(context, NextThatYields(msg)))
        {
            output.Add(m);
        }

        // Assert - all input messages should be yielded through
        Assert.Single(output);
        Assert.Same(msg, output[0]);
    }

    #endregion

    #region Tool Call Accumulation Tests

    [Fact]
    public async Task ExecuteAsync_WithToolCall_AddsToolUseBlock()
    {
        // Arrange
        var context = CreateContext();
        var input = JsonSerializer.Deserialize<JsonElement>("""{"query": "test"}""");
        var messages = new BaseMiddlewareMessage[]
        {
            ModelMsg(new ModelResponseToolCall
            {
                BlockIndex = 0,
                ToolName = "search",
                ToolUseId = "tc-1",
                Input = input,
            }),
        };

        // Act
        await ConsumeAll(_middleware.ExecuteAsync(context, NextThatYields(messages)));

        // Assert
        var assistant = Assert.Single(context.Messages.OfType<InternalAssistantMessage>());
        Assert.Single(assistant.ToolUses);
        Assert.Equal("tc-1", assistant.ToolUses[0].Id);
        Assert.Equal("search", assistant.ToolUses[0].Name);
    }

    #endregion

    #region Thinking Accumulation Tests

    [Fact]
    public async Task ExecuteAsync_WithThinkingContent_AccumulatesThinkingBlock()
    {
        // Arrange
        var context = CreateContext();
        var messages = new BaseMiddlewareMessage[]
        {
            ModelMsg(new ModelResponseThinkingContent { Index = 0, Content = "Let me think..." }),
            ModelMsg(new ModelResponseThinkingContent { Index = 0, Content = " More thinking.", Signature = "sig-1" }),
        };

        // Act
        await ConsumeAll(_middleware.ExecuteAsync(context, NextThatYields(messages)));

        // Assert
        var assistant = Assert.Single(context.Messages.OfType<InternalAssistantMessage>());
        var thinkingBlock = Assert.Single(assistant.ContentBlocks.OfType<InternalThinkingBlock>());
        Assert.Equal("Let me think... More thinking.", thinkingBlock.Text);
        Assert.Equal("sig-1", thinkingBlock.Signature);
    }

    #endregion

    #region Citation Accumulation Tests

    [Fact]
    public async Task ExecuteAsync_WithCitationContent_AddsCitationBlock()
    {
        // Arrange
        var context = CreateContext();
        var messages = new BaseMiddlewareMessage[]
        {
            ModelMsg(new ModelResponseCitationContent
            {
                Title = "Source",
                Url = "https://example.com",
                CitedText = "quoted text",
            }),
        };

        // Act
        await ConsumeAll(_middleware.ExecuteAsync(context, NextThatYields(messages)));

        // Assert
        var assistant = Assert.Single(context.Messages.OfType<InternalAssistantMessage>());
        var citation = Assert.Single(assistant.ContentBlocks.OfType<InternalCitationBlock>());
        Assert.Equal("Source", citation.Title);
        Assert.Equal("https://example.com", citation.Url);
        Assert.Equal("quoted text", citation.CitedText);
    }

    #endregion

    #region Mixed Content Tests

    [Fact]
    public async Task ExecuteAsync_WithMixedContent_PreservesBlockOrder()
    {
        // Arrange
        var context = CreateContext();
        var input = JsonSerializer.Deserialize<JsonElement>("{}");
        var messages = new BaseMiddlewareMessage[]
        {
            ModelMsg(new ModelResponseThinkingContent { Index = 0, Content = "Thinking" }),
            ModelMsg(new ModelResponseTextContent { Content = "Text", BlockIndex = 1 }),
            ModelMsg(new ModelResponseToolCall
            {
                BlockIndex = 2,
                ToolName = "tool",
                ToolUseId = "tc-1",
                Input = input,
            }),
        };

        // Act
        await ConsumeAll(_middleware.ExecuteAsync(context, NextThatYields(messages)));

        // Assert
        var assistant = Assert.Single(context.Messages.OfType<InternalAssistantMessage>());
        Assert.Equal(3, assistant.ContentBlocks.Count);
        Assert.IsType<InternalThinkingBlock>(assistant.ContentBlocks[0]);
        Assert.IsType<InternalTextBlock>(assistant.ContentBlocks[1]);
        Assert.IsType<InternalToolUseBlock>(assistant.ContentBlocks[2]);
    }

    [Fact]
    public async Task ExecuteAsync_WithNoContent_AddsEmptyAssistantMessage()
    {
        // Arrange
        var context = CreateContext();

        // Act
        await ConsumeAll(_middleware.ExecuteAsync(context, NextThatYields()));

        // Assert
        var assistant = Assert.Single(context.Messages.OfType<InternalAssistantMessage>());
        Assert.Null(assistant.TextContent);
        Assert.Empty(assistant.ToolUses);
        Assert.Empty(assistant.ContentBlocks);
    }

    #endregion

    #region PersistMessage Tests

    [Fact]
    public async Task ExecuteAsync_WithPersistMessage_CallsPersistForAssistantMessage()
    {
        // Arrange
        InternalMessage? persistedMessage = null;
        var context = CreateContext();
        context.TurnId = Guid.NewGuid();
        context.PersistMessage = msg =>
        {
            persistedMessage = msg;
            return Task.CompletedTask;
        };
        var messages = new BaseMiddlewareMessage[]
        {
            ModelMsg(new ModelResponseTextContent { Content = "Hello", BlockIndex = 0 }),
        };

        // Act
        await ConsumeAll(_middleware.ExecuteAsync(context, NextThatYields(messages)));

        // Assert
        Assert.NotNull(persistedMessage);
        var assistant = Assert.IsType<InternalAssistantMessage>(persistedMessage);
        Assert.Equal("Hello", assistant.TextContent);
        Assert.Equal(context.TurnId, assistant.TurnId);
    }

    [Fact]
    public async Task ExecuteAsync_WithoutPersistMessage_DoesNotThrow()
    {
        // Arrange
        var context = CreateContext();
        // PersistMessage is null by default
        var messages = new BaseMiddlewareMessage[]
        {
            ModelMsg(new ModelResponseTextContent { Content = "Hello", BlockIndex = 0 }),
        };

        // Act - should not throw
        await ConsumeAll(_middleware.ExecuteAsync(context, NextThatYields(messages)));

        // Assert
        var assistant = Assert.Single(context.Messages.OfType<InternalAssistantMessage>());
        Assert.Equal("Hello", assistant.TextContent);
    }

    #endregion

    #region Server Tool Use Tests

    [Fact]
    public async Task ExecuteAsync_WithServerToolUse_DeduplicatesByBlockIndex()
    {
        // Arrange
        var context = CreateContext();
        var inputJson = JsonSerializer.Deserialize<JsonElement>("""{"url": "https://example.com"}""");
        var messages = new BaseMiddlewareMessage[]
        {
            // First emission (block start, no input)
            ModelMsg(new ModelResponseServerToolUse
            {
                BlockIndex = 0,
                ToolUseId = "stu-1",
                ToolName = "web_search",
                Input = null,
            }),
            // Second emission (block stop, with input)
            ModelMsg(new ModelResponseServerToolUse
            {
                BlockIndex = 0,
                ToolUseId = "stu-1",
                ToolName = "web_search",
                Input = inputJson,
            }),
        };

        // Act
        await ConsumeAll(_middleware.ExecuteAsync(context, NextThatYields(messages)));

        // Assert
        var assistant = Assert.Single(context.Messages.OfType<InternalAssistantMessage>());
        var serverBlock = Assert.Single(assistant.ContentBlocks.OfType<InternalServerToolUseBlock>());
        Assert.Equal("stu-1", serverBlock.Id);
    }

    #endregion

    #region Compaction Accumulation Tests

    [Fact]
    public async Task ExecuteAsync_WithCompactionContent_CreatesCompactionBlock()
    {
        // Arrange
        var context = CreateContext();
        var messages = new BaseMiddlewareMessage[]
        {
            ModelMsg(new ModelResponseCompactionContent
            {
                BlockIndex = 0,
                Summary = "Summary of previous conversation about building a web scraper."
            }),
        };

        // Act
        await ConsumeAll(_middleware.ExecuteAsync(context, NextThatYields(messages)));

        // Assert
        var assistant = Assert.Single(context.Messages.OfType<InternalAssistantMessage>());
        var compaction = Assert.Single(assistant.ContentBlocks.OfType<InternalCompactionBlock>());
        Assert.Equal("Summary of previous conversation about building a web scraper.", compaction.Summary);
    }

    [Fact]
    public async Task ExecuteAsync_WithNullCompactionSummary_CreatesCompactionBlockWithNull()
    {
        // Arrange
        var context = CreateContext();
        var messages = new BaseMiddlewareMessage[]
        {
            ModelMsg(new ModelResponseCompactionContent
            {
                BlockIndex = 0,
                Summary = null
            }),
        };

        // Act
        await ConsumeAll(_middleware.ExecuteAsync(context, NextThatYields(messages)));

        // Assert
        var assistant = Assert.Single(context.Messages.OfType<InternalAssistantMessage>());
        var compaction = Assert.Single(assistant.ContentBlocks.OfType<InternalCompactionBlock>());
        Assert.Null(compaction.Summary);
    }

    [Fact]
    public async Task ExecuteAsync_WithCompactionAndText_PreservesBlockOrder()
    {
        // Arrange
        var context = CreateContext();
        var messages = new BaseMiddlewareMessage[]
        {
            ModelMsg(new ModelResponseCompactionContent
            {
                BlockIndex = 0,
                Summary = "Prior context summary"
            }),
            ModelMsg(new ModelResponseTextContent { Content = "Continuing from where we left off.", BlockIndex = 1 }),
        };

        // Act
        await ConsumeAll(_middleware.ExecuteAsync(context, NextThatYields(messages)));

        // Assert
        var assistant = Assert.Single(context.Messages.OfType<InternalAssistantMessage>());
        Assert.Equal(2, assistant.ContentBlocks.Count);
        Assert.IsType<InternalCompactionBlock>(assistant.ContentBlocks[0]);
        Assert.IsType<InternalTextBlock>(assistant.ContentBlocks[1]);
        Assert.Equal("Prior context summary", ((InternalCompactionBlock)assistant.ContentBlocks[0]).Summary);
        Assert.Equal("Continuing from where we left off.", ((InternalTextBlock)assistant.ContentBlocks[1]).Text);
    }

    [Fact]
    public async Task ExecuteAsync_WithCompactionContent_PassesThroughForStreaming()
    {
        // Arrange
        var context = CreateContext();
        var compactionMsg = ModelMsg(new ModelResponseCompactionContent
        {
            BlockIndex = 0,
            Summary = "Summary"
        });

        // Act
        var output = new List<BaseMiddlewareMessage>();
        await foreach (var m in _middleware.ExecuteAsync(context, NextThatYields(compactionMsg)))
        {
            output.Add(m);
        }

        // Assert - compaction message should be passed through for real-time streaming
        Assert.Single(output);
        Assert.Same(compactionMsg, output[0]);
    }

    [Fact]
    public async Task ExecuteAsync_WithCompactionContent_PersistsInAssistantMessage()
    {
        // Arrange
        InternalMessage? persistedMessage = null;
        var context = CreateContext();
        context.TurnId = Guid.NewGuid();
        context.PersistMessage = msg =>
        {
            persistedMessage = msg;
            return Task.CompletedTask;
        };
        var messages = new BaseMiddlewareMessage[]
        {
            ModelMsg(new ModelResponseCompactionContent
            {
                BlockIndex = 0,
                Summary = "Compacted context"
            }),
            ModelMsg(new ModelResponseTextContent { Content = "Response text", BlockIndex = 1 }),
        };

        // Act
        await ConsumeAll(_middleware.ExecuteAsync(context, NextThatYields(messages)));

        // Assert
        Assert.NotNull(persistedMessage);
        var assistant = Assert.IsType<InternalAssistantMessage>(persistedMessage);
        Assert.Equal(2, assistant.ContentBlocks.Count);
        Assert.IsType<InternalCompactionBlock>(assistant.ContentBlocks[0]);
        Assert.Equal(context.TurnId, assistant.TurnId);
    }

    #endregion

    #region Helpers

    private static ModelMiddlewareContext CreateContext()
    {
        return new ModelMiddlewareContext
        {
            ProviderOptions = new ProviderOptions
            {
                ApiKey = "test-key",
                ModelId = "test-model",
            },
        };
    }

    private static ModelMiddlewareMessage ModelMsg(ModelResponseBase response) =>
        new() { ModelMessage = response };

    private static Func<ModelMiddlewareContext, IAsyncEnumerable<BaseMiddlewareMessage>> NextThatYields(
        params BaseMiddlewareMessage[] messages)
    {
        return _ => YieldMessages(messages);
    }

    private static async IAsyncEnumerable<BaseMiddlewareMessage> YieldMessages(BaseMiddlewareMessage[] messages)
    {
        foreach (var msg in messages)
        {
            yield return msg;
        }
        await Task.CompletedTask;
    }

    private static async Task ConsumeAll(IAsyncEnumerable<BaseMiddlewareMessage> stream)
    {
        await foreach (var _ in stream) { }
    }

    #endregion
}
