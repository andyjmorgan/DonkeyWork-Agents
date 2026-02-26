using DonkeyWork.Agents.Orleans.Core.Middleware.Messages;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Orleans.Core.Middleware;

internal sealed class ExceptionMiddleware : IModelMiddleware
{
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(ILogger<ExceptionMiddleware> logger) => _logger = logger;

    public async IAsyncEnumerable<BaseMiddlewareMessage> ExecuteAsync(
        ModelMiddlewareContext context,
        Func<ModelMiddlewareContext, IAsyncEnumerable<BaseMiddlewareMessage>> next)
    {
        _logger.LogDebug("ExceptionMiddleware entering");

        IAsyncEnumerator<BaseMiddlewareMessage>? enumerator = null;
        try
        {
            enumerator = next(context).GetAsyncEnumerator(context.CancellationToken);

            while (true)
            {
                bool moved;
                ErrorMessage? errorMessage = null;

                try
                {
                    moved = await enumerator.MoveNextAsync();
                }
                catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested)
                {
                    throw; // Let cancellation propagate
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "ExceptionMiddleware caught pipeline exception: {Message}", ex.Message);
                    errorMessage = new ErrorMessage
                    {
                        ErrorText = ExtractErrorMessage(ex),
                        Exception = ex
                    };
                    moved = false;
                }

                if (errorMessage is not null)
                {
                    yield return errorMessage;
                    yield break;
                }

                if (!moved)
                    yield break;

                yield return enumerator.Current;
            }
        }
        finally
        {
            _logger.LogDebug("ExceptionMiddleware exiting");
            if (enumerator is not null)
                await enumerator.DisposeAsync();
        }
    }

    private static string ExtractErrorMessage(Exception ex)
    {
        var messages = new List<string>();
        var current = ex;
        while (current is not null)
        {
            if (!string.IsNullOrWhiteSpace(current.Message))
                messages.Add(current.Message);
            current = current.InnerException;
        }

        return messages.Count > 0
            ? string.Join(" -> ", messages)
            : "An unknown error occurred";
    }
}
