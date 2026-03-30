using DonkeyWork.Agents.Actors.Core.Middleware.Messages;
using Microsoft.Extensions.DependencyInjection;

namespace DonkeyWork.Agents.Actors.Core.Middleware;

public sealed class ModelPipeline
{
    private readonly IServiceProvider _services;

    // Standard order: Exception -> Tool -> Guardrails -> Accumulator -> UsageTracking -> Provider
    private static readonly Type[] StandardPipeline =
    [
        typeof(ExceptionMiddleware),
        typeof(ToolMiddleware),
        typeof(GuardrailsMiddleware),
        typeof(AccumulatorMiddleware),
        typeof(UsageTrackingMiddleware),
        typeof(ProviderMiddleware)
    ];

    public ModelPipeline(IServiceProvider services)
    {
        _services = services;
    }

    internal IAsyncEnumerable<BaseMiddlewareMessage> ExecuteAsync(ModelMiddlewareContext context)
    {
        var pipeline = BuildPipeline(StandardPipeline);
        return pipeline(context);
    }

    private Func<ModelMiddlewareContext, IAsyncEnumerable<BaseMiddlewareMessage>> BuildPipeline(
        Type[] middlewareTypes)
    {
        Func<ModelMiddlewareContext, IAsyncEnumerable<BaseMiddlewareMessage>>? current = null;

        for (int i = middlewareTypes.Length - 1; i >= 0; i--)
        {
            var middleware = (IModelMiddleware)ActivatorUtilities.CreateInstance(_services, middlewareTypes[i]);
            var next = current;

            if (next is null)
            {
                // Innermost middleware (ProviderMiddleware) — next is a no-op that should never be called
                current = ctx => middleware.ExecuteAsync(ctx, _ => EmptyStream());
            }
            else
            {
                var capturedNext = next;
                current = ctx => middleware.ExecuteAsync(ctx, capturedNext);
            }
        }

        return current ?? (_ => EmptyStream());
    }

    private static async IAsyncEnumerable<BaseMiddlewareMessage> EmptyStream()
    {
        yield break;
    }
}
