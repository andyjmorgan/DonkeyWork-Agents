using DonkeyWork.Agents.Actors.Core.Middleware.Messages;
using DonkeyWork.Agents.Actors.Core.Providers.Responses;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Actors.Core.Middleware;

internal sealed class UsageTrackingMiddleware : IModelMiddleware
{
    private readonly ILogger<UsageTrackingMiddleware> _logger;

    public UsageTrackingMiddleware(ILogger<UsageTrackingMiddleware> logger) => _logger = logger;

    public async IAsyncEnumerable<BaseMiddlewareMessage> ExecuteAsync(
        ModelMiddlewareContext context,
        Func<ModelMiddlewareContext, IAsyncEnumerable<BaseMiddlewareMessage>> next)
    {
        int totalInputTokens = 0;
        int totalOutputTokens = 0;
        int totalCachedInputTokens = 0;
        int totalWebSearchRequests = 0;
        int totalWebFetchRequests = 0;
        int usageEventCount = 0;

        await foreach (var msg in next(context))
        {
            if (msg is ModelMiddlewareMessage { ModelMessage: ModelResponseUsage usage })
            {
                usageEventCount++;
                totalInputTokens += usage.InputTokens;
                totalOutputTokens += usage.OutputTokens;
                totalCachedInputTokens += usage.CachedInputTokens;
                totalWebSearchRequests += usage.WebSearchRequests;
                totalWebFetchRequests += usage.WebFetchRequests;

                _logger.LogInformation(
                    "Usage event #{EventNum}: input={InputTokens}, output={OutputTokens}, cached={CachedInputTokens}, webSearch={WebSearch}, webFetch={WebFetch}",
                    usageEventCount, usage.InputTokens, usage.OutputTokens, usage.CachedInputTokens,
                    usage.WebSearchRequests, usage.WebFetchRequests);
            }

            yield return msg;
        }

        if (usageEventCount > 0)
        {
            _logger.LogInformation(
                "Usage totals ({EventCount} events): input={TotalInput}, output={TotalOutput}, cached={TotalCached}, webSearch={TotalWebSearch}, webFetch={TotalWebFetch}",
                usageEventCount, totalInputTokens, totalOutputTokens, totalCachedInputTokens,
                totalWebSearchRequests, totalWebFetchRequests);
        }
    }
}
