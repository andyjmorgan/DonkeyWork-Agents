using System.Runtime.CompilerServices;
using DonkeyWork.Agents.Providers.Core.Middleware.Internal;
using DonkeyWork.Agents.Providers.Core.Middleware.Internal.Messages;
using DonkeyWork.Agents.Providers.Core.Providers;

namespace DonkeyWork.Agents.Providers.Core.Middleware;

/// <summary>
/// Middleware that calls the AI provider.
/// </summary>
internal class ProviderMiddleware : IModelMiddleware
{
    private readonly IAiClientFactory _aiClientFactory;

    public ProviderMiddleware(IAiClientFactory aiClientFactory)
    {
        _aiClientFactory = aiClientFactory;
    }

    public async IAsyncEnumerable<BaseMiddlewareMessage> ExecuteAsync(
        ModelMiddlewareContext context,
        Func<ModelMiddlewareContext, IAsyncEnumerable<BaseMiddlewareMessage>> next,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var aiClient = await _aiClientFactory.CreateClientAsync(context.Model, cancellationToken);

        await foreach (var message in aiClient.StreamCompletionAsync(
            context.Messages,
            context.ToolContext?.Tools,
            context.ProviderParameters,
            cancellationToken))
        {
            yield return new ModelMiddlewareMessage
            {
                ModelMessage = message
            };
        }
    }
}
