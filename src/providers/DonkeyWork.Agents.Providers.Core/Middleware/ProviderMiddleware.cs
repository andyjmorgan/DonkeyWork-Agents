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

        // Check if streaming is enabled (default is true for backward compatibility)
        var shouldStream = true;
        if (context.ProviderParameters.TryGetValue("stream", out var streamValue))
        {
            shouldStream = streamValue switch
            {
                bool b => b,
                string s => !string.Equals(s, "false", StringComparison.OrdinalIgnoreCase),
                _ => true
            };
        }

        // Select the appropriate completion method based on streaming preference
        var responseStream = shouldStream
            ? aiClient.StreamCompletionAsync(
                context.Messages,
                context.ToolContext?.Tools,
                context.ProviderParameters,
                cancellationToken)
            : aiClient.CompleteAsync(
                context.Messages,
                context.ToolContext?.Tools,
                context.ProviderParameters,
                cancellationToken);

        await foreach (var message in responseStream)
        {
            yield return new ModelMiddlewareMessage
            {
                ModelMessage = message
            };
        }
    }
}
