using System.Runtime.CompilerServices;
using DonkeyWork.Agents.Providers.Core.Middleware.Internal;
using DonkeyWork.Agents.Providers.Core.Middleware.Internal.Messages;

namespace DonkeyWork.Agents.Providers.Core.Middleware;

/// <summary>
/// Middleware that accumulates streamed content into a full assistant message.
/// </summary>
internal class AccumulatorMiddleware : IModelMiddleware
{
    public async IAsyncEnumerable<BaseMiddlewareMessage> ExecuteAsync(
        ModelMiddlewareContext context,
        Func<ModelMiddlewareContext, IAsyncEnumerable<BaseMiddlewareMessage>> next,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // TODO: Implement full accumulation logic
        await foreach (var message in next(context).WithCancellation(cancellationToken))
        {
            yield return message;
        }
    }
}
