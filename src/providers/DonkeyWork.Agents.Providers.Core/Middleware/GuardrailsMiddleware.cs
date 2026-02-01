using System.Runtime.CompilerServices;
using DonkeyWork.Agents.Providers.Core.Middleware.Internal;
using DonkeyWork.Agents.Providers.Core.Middleware.Internal.Messages;

namespace DonkeyWork.Agents.Providers.Core.Middleware;

/// <summary>
/// Middleware for security and compliance checks.
/// Currently a passthrough.
/// </summary>
internal class GuardrailsMiddleware : IModelMiddleware
{
    public async IAsyncEnumerable<BaseMiddlewareMessage> ExecuteAsync(
        ModelMiddlewareContext context,
        Func<ModelMiddlewareContext, IAsyncEnumerable<BaseMiddlewareMessage>> next,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var message in next(context).WithCancellation(cancellationToken))
        {
            yield return message;
        }
    }
}
