using DonkeyWork.Agents.Actors.Core.Middleware.Messages;

namespace DonkeyWork.Agents.Actors.Core.Middleware;

internal interface IModelMiddleware
{
    IAsyncEnumerable<BaseMiddlewareMessage> ExecuteAsync(
        ModelMiddlewareContext context,
        Func<ModelMiddlewareContext, IAsyncEnumerable<BaseMiddlewareMessage>> next);
}
