using System.Text.Json;
using DonkeyWork.Agents.Actors.Contracts.Messages;
using DonkeyWork.Agents.Actors.Core.Middleware;
using DonkeyWork.Agents.Actors.Core.Middleware.Messages;
using DonkeyWork.Agents.Actors.Core.Providers;
using DonkeyWork.Agents.Actors.Core.Providers.Responses;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DonkeyWork.Agents.Actors.Tests.Middleware;

public class ToolMiddlewareTests
{
    private readonly ToolMiddleware _middleware;

    public ToolMiddlewareTests()
    {
        _middleware = new ToolMiddleware(NullLogger<ToolMiddleware>.Instance);
    }

    #region No Executor Tests

    [Fact]
    public async Task ExecuteAsync_WithNoExecutor_PassesThroughMessages()
    {
        // Arrange
        var context = CreateContext(toolExecutor: null);
        var expected = ModelMsg(new ModelResponseTextContent { Content = "Hello", BlockIndex = 0 });

        // Act
        var messages = await CollectAll(_middleware.ExecuteAsync(context, NextThatYields(expected)));

        // Assert
        Assert.Single(messages);
        Assert.Same(expected, messages[0]);
    }

    #endregion

    #region No Tool Calls Tests

    [Fact]
    public async Task ExecuteAsync_WithNoToolCalls_YieldsMessagesAndStops()
    {
        // Arrange
        var executor = new FakeToolExecutor();
        var context = CreateContext(toolExecutor: executor);
        var textMsg = ModelMsg(new ModelResponseTextContent { Content = "Hello", BlockIndex = 0 });

        // Act
        var messages = await CollectAll(_middleware.ExecuteAsync(context, NextThatYields(textMsg)));

        // Assert
        Assert.Single(messages);
        Assert.Equal(0, executor.ExecutionCount);
    }

    #endregion

    #region Tool Execution Tests

    [Fact]
    public async Task ExecuteAsync_WithToolCall_ExecutesToolAndYieldsResponse()
    {
        // Arrange
        var executor = new FakeToolExecutor();
        executor.SetResult("search", new ToolExecutionResult("search result"));

        var context = CreateContext(toolExecutor: executor);
        var input = JsonSerializer.Deserialize<JsonElement>("""{"query": "test"}""");
        var toolCallMsg = ModelMsg(new ModelResponseToolCall
        {
            BlockIndex = 0,
            ToolName = "search",
            ToolUseId = "tc-1",
            Input = input,
        });

        int callCount = 0;
        Func<ModelMiddlewareContext, IAsyncEnumerable<BaseMiddlewareMessage>> next = ctx =>
        {
            callCount++;
            if (callCount == 1)
                return YieldMessages([toolCallMsg]);
            return YieldMessages([ModelMsg(new ModelResponseTextContent { Content = "Final answer", BlockIndex = 0 })]);
        };

        // Act
        var messages = await CollectAll(_middleware.ExecuteAsync(context, next));

        // Assert
        Assert.Equal(1, executor.ExecutionCount);
        Assert.Contains(messages, m => m is ToolResponseMessage { ToolName: "search", Success: true });
    }

    [Fact]
    public async Task ExecuteAsync_WithToolError_YieldsErrorResponse()
    {
        // Arrange
        var executor = new FakeToolExecutor();
        executor.SetResult("broken", new ToolExecutionResult("tool failed", IsError: true));

        var context = CreateContext(toolExecutor: executor);
        var input = JsonSerializer.Deserialize<JsonElement>("{}");
        var toolCallMsg = ModelMsg(new ModelResponseToolCall
        {
            BlockIndex = 0,
            ToolName = "broken",
            ToolUseId = "tc-1",
            Input = input,
        });

        int callCount = 0;
        Func<ModelMiddlewareContext, IAsyncEnumerable<BaseMiddlewareMessage>> next = ctx =>
        {
            callCount++;
            if (callCount == 1)
                return YieldMessages([toolCallMsg]);
            return YieldMessages([ModelMsg(new ModelResponseTextContent { Content = "Done", BlockIndex = 0 })]);
        };

        // Act
        var messages = await CollectAll(_middleware.ExecuteAsync(context, next));

        // Assert
        var toolResponse = messages.OfType<ToolResponseMessage>().Single();
        Assert.False(toolResponse.Success);
        Assert.Equal("tool failed", toolResponse.Response);
    }

    [Fact]
    public async Task ExecuteAsync_WhenToolThrowsException_YieldsErrorResponse()
    {
        // Arrange
        var executor = new FakeToolExecutor();
        executor.SetException("throws", new InvalidOperationException("tool explosion"));

        var context = CreateContext(toolExecutor: executor);
        var input = JsonSerializer.Deserialize<JsonElement>("{}");
        var toolCallMsg = ModelMsg(new ModelResponseToolCall
        {
            BlockIndex = 0,
            ToolName = "throws",
            ToolUseId = "tc-1",
            Input = input,
        });

        int callCount = 0;
        Func<ModelMiddlewareContext, IAsyncEnumerable<BaseMiddlewareMessage>> next = ctx =>
        {
            callCount++;
            if (callCount == 1)
                return YieldMessages([toolCallMsg]);
            return YieldMessages([ModelMsg(new ModelResponseTextContent { Content = "Done", BlockIndex = 0 })]);
        };

        // Act
        var messages = await CollectAll(_middleware.ExecuteAsync(context, next));

        // Assert
        var toolResponse = messages.OfType<ToolResponseMessage>().Single();
        Assert.False(toolResponse.Success);
        Assert.Contains("tool explosion", toolResponse.Response);
    }

    [Fact]
    public async Task ExecuteAsync_WithMultipleToolCalls_ExecutesAllInParallel()
    {
        // Arrange
        var executor = new FakeToolExecutor();
        executor.SetDefaultResult(new ToolExecutionResult("result"));

        var context = CreateContext(toolExecutor: executor);
        var input = JsonSerializer.Deserialize<JsonElement>("{}");
        var tc1 = ModelMsg(new ModelResponseToolCall { BlockIndex = 0, ToolName = "tool1", ToolUseId = "tc-1", Input = input });
        var tc2 = ModelMsg(new ModelResponseToolCall { BlockIndex = 1, ToolName = "tool2", ToolUseId = "tc-2", Input = input });

        int callCount = 0;
        Func<ModelMiddlewareContext, IAsyncEnumerable<BaseMiddlewareMessage>> next = ctx =>
        {
            callCount++;
            if (callCount == 1)
                return YieldMessages([tc1, tc2]);
            return YieldMessages([ModelMsg(new ModelResponseTextContent { Content = "Done", BlockIndex = 0 })]);
        };

        // Act
        var messages = await CollectAll(_middleware.ExecuteAsync(context, next));

        // Assert
        Assert.Equal(2, executor.ExecutionCount);
        var toolResponses = messages.OfType<ToolResponseMessage>().ToList();
        Assert.Equal(2, toolResponses.Count);
    }

    [Fact]
    public async Task ExecuteAsync_AddsToolResultMessagesToContext()
    {
        // Arrange
        var executor = new FakeToolExecutor();
        executor.SetResult("search", new ToolExecutionResult("result content"));

        var context = CreateContext(toolExecutor: executor);
        var input = JsonSerializer.Deserialize<JsonElement>("{}");
        var toolCallMsg = ModelMsg(new ModelResponseToolCall
        {
            BlockIndex = 0,
            ToolName = "search",
            ToolUseId = "tc-1",
            Input = input,
        });

        int callCount = 0;
        Func<ModelMiddlewareContext, IAsyncEnumerable<BaseMiddlewareMessage>> next = ctx =>
        {
            callCount++;
            if (callCount == 1)
                return YieldMessages([toolCallMsg]);
            return YieldMessages([ModelMsg(new ModelResponseTextContent { Content = "Final", BlockIndex = 0 })]);
        };

        // Act
        await CollectAll(_middleware.ExecuteAsync(context, next));

        // Assert
        var toolResult = context.Messages.OfType<InternalToolResultMessage>().Single();
        Assert.Equal("tc-1", toolResult.ToolUseId);
        Assert.Equal("result content", toolResult.Content);
        Assert.False(toolResult.IsError);
    }

    #endregion

    #region Cancellation Tests

    [Fact]
    public async Task ExecuteAsync_WhenCancelled_StopsIteration()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var executor = new FakeToolExecutor();
        var context = CreateContext(toolExecutor: executor, ct: cts.Token);

        // Cancel immediately
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await CollectAll(_middleware.ExecuteAsync(context, NextThatYields()));
        });
    }

    #endregion

    #region Helpers

    private static ModelMiddlewareContext CreateContext(IToolExecutor? toolExecutor = null, CancellationToken ct = default)
    {
        return new ModelMiddlewareContext
        {
            ProviderOptions = new ProviderOptions
            {
                ApiKey = "test-key",
                ModelId = "test-model",
            },
            ToolExecutor = toolExecutor,
            CancellationToken = ct,
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

    private static async Task<List<BaseMiddlewareMessage>> CollectAll(IAsyncEnumerable<BaseMiddlewareMessage> stream)
    {
        var list = new List<BaseMiddlewareMessage>();
        await foreach (var msg in stream)
        {
            list.Add(msg);
        }
        return list;
    }

    /// <summary>
    /// Simple test double for IToolExecutor since it's internal and can't be mocked by Castle.DynamicProxy.
    /// </summary>
    private sealed class FakeToolExecutor : IToolExecutor
    {
        private readonly Dictionary<string, ToolExecutionResult> _results = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Exception> _exceptions = new(StringComparer.OrdinalIgnoreCase);
        private ToolExecutionResult? _defaultResult;
        private int _executionCount;

        public int ExecutionCount => _executionCount;

        public void SetResult(string toolName, ToolExecutionResult result) => _results[toolName] = result;
        public void SetException(string toolName, Exception ex) => _exceptions[toolName] = ex;
        public void SetDefaultResult(ToolExecutionResult result) => _defaultResult = result;

        public Task<ToolExecutionResult> ExecuteAsync(string toolName, JsonElement arguments, CancellationToken ct)
        {
            Interlocked.Increment(ref _executionCount);

            if (_exceptions.TryGetValue(toolName, out var ex))
                throw ex;

            if (_results.TryGetValue(toolName, out var result))
                return Task.FromResult(result);

            if (_defaultResult is not null)
                return Task.FromResult(_defaultResult);

            return Task.FromResult(new ToolExecutionResult($"No result configured for {toolName}", IsError: true));
        }
    }

    #endregion
}
