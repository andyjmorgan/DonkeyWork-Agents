using System.Diagnostics;
using DonkeyWork.Agents.Actors.Contracts.Messages;
using DonkeyWork.Agents.Actors.Core.Middleware.Messages;
using DonkeyWork.Agents.Actors.Core.Providers;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Actors.Core.Middleware;

internal sealed class ProviderMiddleware : IModelMiddleware
{
    private readonly IAiProviderFactory _providerFactory;
    private readonly ILogger<ProviderMiddleware> _logger;

    public ProviderMiddleware(IAiProviderFactory providerFactory, ILogger<ProviderMiddleware> logger)
    {
        _providerFactory = providerFactory;
        _logger = logger;
    }

    public async IAsyncEnumerable<BaseMiddlewareMessage> ExecuteAsync(
        ModelMiddlewareContext context,
        Func<ModelMiddlewareContext, IAsyncEnumerable<BaseMiddlewareMessage>> next)
    {
        // Terminal middleware — does not call next()
        var provider = _providerFactory.Create(ProviderType.Anthropic);

        var systemPrompt = context.SystemPrompt;
        if (string.IsNullOrEmpty(systemPrompt))
        {
            var sysMsg = context.Messages
                .OfType<InternalContentMessage>()
                .FirstOrDefault(m => m.Role == InternalMessageRole.System);
            systemPrompt = sysMsg?.Content ?? "";
        }

        var nonSystemMessages = context.Messages
            .Where(m => m is not InternalContentMessage { Role: InternalMessageRole.System })
            .ToList();

        var toolCount = context.Tools?.Count ?? 0;
        var streaming = context.ProviderOptions?.Stream ?? true;
        _logger.LogInformation(
            "LLM request: {MessageCount} messages, {ToolCount} tools, model={Model}, stream={Stream}",
            nonSystemMessages.Count, toolCount, context.ProviderOptions?.ModelId ?? "default", streaming);

        var sw = Stopwatch.StartNew();
        int responseCount = 0;
        await foreach (var response in provider.StreamCompletionAsync(
            systemPrompt,
            nonSystemMessages,
            context.Tools,
            context.ProviderOptions!,
            context.CancellationToken))
        {
            responseCount++;
            yield return new ModelMiddlewareMessage { ModelMessage = response };
        }
        sw.Stop();

        _logger.LogInformation("LLM response: {Count} chunks in {ElapsedMs}ms", responseCount, sw.ElapsedMilliseconds);
    }
}
