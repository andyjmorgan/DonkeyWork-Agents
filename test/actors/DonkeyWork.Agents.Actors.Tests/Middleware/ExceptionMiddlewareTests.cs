using DonkeyWork.Agents.Actors.Core.Middleware;
using DonkeyWork.Agents.Actors.Core.Middleware.Messages;
using DonkeyWork.Agents.Actors.Core.Providers;
using DonkeyWork.Agents.Actors.Core.Providers.Responses;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DonkeyWork.Agents.Actors.Tests.Middleware;

public class ExceptionMiddlewareTests
{
    private readonly ExceptionMiddleware _middleware;

    public ExceptionMiddlewareTests()
    {
        _middleware = new ExceptionMiddleware(NullLogger<ExceptionMiddleware>.Instance);
    }

    #region Normal Flow Tests

    [Fact]
    public async Task ExecuteAsync_WithNoException_PassesThroughMessages()
    {
        // Arrange
        var context = CreateContext();
        var expected = new ModelMiddlewareMessage
        {
            ModelMessage = new ModelResponseTextContent { Content = "Hello", BlockIndex = 0 }
        };

        // Act
        var messages = new List<BaseMiddlewareMessage>();
        await foreach (var msg in _middleware.ExecuteAsync(context, NextThatYields(expected)))
        {
            messages.Add(msg);
        }

        // Assert
        Assert.Single(messages);
        Assert.Same(expected, messages[0]);
    }

    [Fact]
    public async Task ExecuteAsync_WithMultipleMessages_PassesAllThrough()
    {
        // Arrange
        var context = CreateContext();
        var msg1 = new ModelMiddlewareMessage
        {
            ModelMessage = new ModelResponseTextContent { Content = "Hello", BlockIndex = 0 }
        };
        var msg2 = new ModelMiddlewareMessage
        {
            ModelMessage = new ModelResponseTextContent { Content = " World", BlockIndex = 0 }
        };

        // Act
        var messages = new List<BaseMiddlewareMessage>();
        await foreach (var msg in _middleware.ExecuteAsync(context, NextThatYields(msg1, msg2)))
        {
            messages.Add(msg);
        }

        // Assert
        Assert.Equal(2, messages.Count);
    }

    #endregion

    #region Exception Handling Tests

    [Fact]
    public async Task ExecuteAsync_WhenNextThrowsImmediately_YieldsErrorMessage()
    {
        // Arrange
        var context = CreateContext();

        static async IAsyncEnumerable<BaseMiddlewareMessage> throwingNext(ModelMiddlewareContext _)
        {
            await Task.CompletedTask;
            throw new InvalidOperationException("test error");
            yield break; // unreachable but needed for compiler
        }

        // Act
        var messages = new List<BaseMiddlewareMessage>();
        await foreach (var msg in _middleware.ExecuteAsync(context, throwingNext))
        {
            messages.Add(msg);
        }

        // Assert
        Assert.Single(messages);
        var error = Assert.IsType<ErrorMessage>(messages[0]);
        Assert.Contains("test error", error.ErrorText);
        Assert.IsType<InvalidOperationException>(error.Exception);
    }

    [Fact]
    public async Task ExecuteAsync_WhenNextThrowsMidStream_YieldsPartialThenError()
    {
        // Arrange
        var context = CreateContext();

        static async IAsyncEnumerable<BaseMiddlewareMessage> throwingMidStreamNext(ModelMiddlewareContext _)
        {
            yield return new ModelMiddlewareMessage
            {
                ModelMessage = new ModelResponseTextContent { Content = "partial", BlockIndex = 0 }
            };
            await Task.CompletedTask;
            throw new InvalidOperationException("mid-stream error");
        }

        // Act
        var messages = new List<BaseMiddlewareMessage>();
        await foreach (var msg in _middleware.ExecuteAsync(context, throwingMidStreamNext))
        {
            messages.Add(msg);
        }

        // Assert
        Assert.Equal(2, messages.Count);
        Assert.IsType<ModelMiddlewareMessage>(messages[0]);
        var error = Assert.IsType<ErrorMessage>(messages[1]);
        Assert.Contains("mid-stream error", error.ErrorText);
    }

    [Fact]
    public async Task ExecuteAsync_WithNestedExceptions_ConcatenatesMessages()
    {
        // Arrange
        var context = CreateContext();

        static async IAsyncEnumerable<BaseMiddlewareMessage> nestedThrowingNext(ModelMiddlewareContext _)
        {
            await Task.CompletedTask;
            throw new InvalidOperationException("outer", new ArgumentException("inner"));
            yield break;
        }

        // Act
        var messages = new List<BaseMiddlewareMessage>();
        await foreach (var msg in _middleware.ExecuteAsync(context, nestedThrowingNext))
        {
            messages.Add(msg);
        }

        // Assert
        var error = Assert.IsType<ErrorMessage>(messages[0]);
        Assert.Contains("outer", error.ErrorText);
        Assert.Contains("inner", error.ErrorText);
    }

    #endregion

    #region Cancellation Tests

    [Fact]
    public async Task ExecuteAsync_WhenCancelled_PropagatesCancellation()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var context = CreateContext(cts.Token);

        static async IAsyncEnumerable<BaseMiddlewareMessage> cancellingNext(ModelMiddlewareContext ctx)
        {
            await Task.CompletedTask;
            ctx.CancellationToken.ThrowIfCancellationRequested();
            yield break;
        }

        // Act & Assert
        cts.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await foreach (var _ in _middleware.ExecuteAsync(context, cancellingNext))
            {
            }
        });
    }

    #endregion

    #region Empty Stream Tests

    [Fact]
    public async Task ExecuteAsync_WithEmptyStream_ReturnsNoMessages()
    {
        // Arrange
        var context = CreateContext();

        static async IAsyncEnumerable<BaseMiddlewareMessage> emptyNext(ModelMiddlewareContext _)
        {
            await Task.CompletedTask;
            yield break;
        }

        // Act
        var messages = new List<BaseMiddlewareMessage>();
        await foreach (var msg in _middleware.ExecuteAsync(context, emptyNext))
        {
            messages.Add(msg);
        }

        // Assert
        Assert.Empty(messages);
    }

    #endregion

    #region Helpers

    private static ModelMiddlewareContext CreateContext(CancellationToken ct = default)
    {
        return new ModelMiddlewareContext
        {
            ProviderOptions = new ProviderOptions
            {
                ApiKey = "test-key",
                ModelId = "test-model",
            },
            CancellationToken = ct,
        };
    }

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

    #endregion
}
