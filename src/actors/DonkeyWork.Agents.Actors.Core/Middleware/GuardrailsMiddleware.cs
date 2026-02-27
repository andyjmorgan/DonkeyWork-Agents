using DonkeyWork.Agents.Actors.Core.Middleware.Messages;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Actors.Core.Middleware;

internal sealed class GuardrailsMiddleware : IModelMiddleware
{
    private readonly ILogger<GuardrailsMiddleware> _logger;

    public GuardrailsMiddleware(ILogger<GuardrailsMiddleware> logger) => _logger = logger;

    public async IAsyncEnumerable<BaseMiddlewareMessage> ExecuteAsync(
        ModelMiddlewareContext context,
        Func<ModelMiddlewareContext, IAsyncEnumerable<BaseMiddlewareMessage>> next)
    {
        _logger.LogDebug("GuardrailsMiddleware entering (stub pass-through)");

        await foreach (var msg in next(context))
            yield return msg;

        _logger.LogDebug("GuardrailsMiddleware exiting");
    }
}
