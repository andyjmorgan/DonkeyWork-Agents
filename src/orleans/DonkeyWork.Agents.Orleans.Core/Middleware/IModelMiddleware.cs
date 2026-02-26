using DonkeyWork.Agents.Orleans.Core.Middleware.Messages;

namespace DonkeyWork.Agents.Orleans.Core.Middleware;

internal interface IModelMiddleware
{
    IAsyncEnumerable<BaseMiddlewareMessage> ExecuteAsync(
        ModelMiddlewareContext context,
        Func<ModelMiddlewareContext, IAsyncEnumerable<BaseMiddlewareMessage>> next);
}
