using DonkeyWork.Agents.Providers.Core.Middleware.Internal.Messages;

namespace DonkeyWork.Agents.Providers.Core.Middleware.Internal;

/// <summary>
/// Internal interface for middleware in the model execution pipeline.
/// </summary>
internal interface IModelMiddleware
{
    /// <summary>
    /// Executes the middleware, optionally calling the next middleware in the chain.
    /// </summary>
    IAsyncEnumerable<BaseMiddlewareMessage> ExecuteAsync(
        ModelMiddlewareContext context,
        Func<ModelMiddlewareContext, IAsyncEnumerable<BaseMiddlewareMessage>> next,
        CancellationToken cancellationToken = default);
}
