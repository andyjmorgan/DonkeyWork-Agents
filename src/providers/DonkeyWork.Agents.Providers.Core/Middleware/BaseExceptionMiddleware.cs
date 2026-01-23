using System.Runtime.CompilerServices;
using DonkeyWork.Agents.Providers.Core.Middleware.Internal;
using DonkeyWork.Agents.Providers.Core.Middleware.Internal.Messages;
using DonkeyWork.Agents.Providers.Core.Middleware.Internal.Responses;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Providers.Core.Middleware;

/// <summary>
/// Middleware that wraps the pipeline with exception handling.
/// </summary>
internal class BaseExceptionMiddleware : IModelMiddleware
{
    private readonly ILogger<BaseExceptionMiddleware> _logger;

    public BaseExceptionMiddleware(ILogger<BaseExceptionMiddleware> logger)
    {
        _logger = logger;
    }

    public IAsyncEnumerable<BaseMiddlewareMessage> ExecuteAsync(
        ModelMiddlewareContext context,
        Func<ModelMiddlewareContext, IAsyncEnumerable<BaseMiddlewareMessage>> next)
    {
        return WrapWithHandlerAsync(next, context, HandleException);
    }

    private BaseMiddlewareMessage HandleException(Exception exception, ModelMiddlewareContext context)
    {
        _logger.LogError(exception, "An error occurred while processing the middleware request.");
        var errorMessage = BuildDetailedErrorMessage(exception);

        return new ModelMiddlewareMessage
        {
            ModelMessage = new ModelResponseErrorContent
            {
                ErrorMessage = errorMessage,
                Exception = exception
            }
        };
    }

    private static string BuildDetailedErrorMessage(Exception exception)
    {
        return exception switch
        {
            OperationCanceledException => "The operation was cancelled.",
            TimeoutException => "The operation timed out.",
            _ => $"An error occurred: {exception.Message}"
        };
    }

    private static async IAsyncEnumerable<BaseMiddlewareMessage> WrapWithHandlerAsync(
        Func<ModelMiddlewareContext, IAsyncEnumerable<BaseMiddlewareMessage>> next,
        ModelMiddlewareContext context,
        Func<Exception, ModelMiddlewareContext, BaseMiddlewareMessage> onException,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await using var enumerator = next(context).GetAsyncEnumerator(cancellationToken);

        while (true)
        {
            BaseMiddlewareMessage? result = null;
            bool hasResult;
            Exception? caughtException = null;

            try
            {
                hasResult = await enumerator.MoveNextAsync().ConfigureAwait(false);
                result = hasResult ? enumerator.Current : null;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                caughtException = ex;
                hasResult = false;
            }

            if (caughtException != null)
            {
                yield return onException(caughtException, context);
                yield break;
            }

            if (!hasResult)
            {
                break;
            }

            if (result != null)
            {
                yield return result;
            }
        }
    }
}
