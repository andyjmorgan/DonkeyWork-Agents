using System.Diagnostics;
using System.Net.Http;
using Anthropic.Exceptions;
using DonkeyWork.Agents.Actors.Contracts.Messages;
using DonkeyWork.Agents.Actors.Core.Middleware.Messages;
using DonkeyWork.Agents.Actors.Core.Providers;
using DonkeyWork.Agents.Actors.Core.Providers.Responses;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Actors.Core.Middleware;

internal sealed class ProviderMiddleware : IModelMiddleware
{
    private const int MaxRetries = 5;

    private static readonly int[] BackoffMs = [1000, 2000, 4000, 8000, 16000];

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

        for (int attempt = 0; attempt <= MaxRetries; attempt++)
        {
            bool shouldRetry = false;
            string? retryReason = null;
            IAsyncEnumerator<ModelResponseBase>? enumerator = null;

            try
            {
                enumerator = provider.StreamCompletionAsync(
                    systemPrompt,
                    nonSystemMessages,
                    context.Tools,
                    context.ProviderOptions!,
                    context.CancellationToken).GetAsyncEnumerator(context.CancellationToken);

                while (true)
                {
                    bool moved;
                    try
                    {
                        moved = await enumerator.MoveNextAsync();
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex) when (IsTransient(ex) && responseCount == 0 && attempt < MaxRetries)
                    {
                        shouldRetry = true;
                        retryReason = ExtractRetryReason(ex);
                        _logger.LogWarning(ex,
                            "Transient LLM error on attempt {Attempt}/{MaxRetries}, will retry: {Reason}",
                            attempt + 1, MaxRetries + 1, retryReason);
                        break;
                    }

                    if (!moved) break;

                    responseCount++;
                    yield return new ModelMiddlewareMessage { ModelMessage = enumerator.Current };
                }
            }
            finally
            {
                if (enumerator is not null)
                    await enumerator.DisposeAsync();
            }

            if (!shouldRetry)
                break;

            var delayMs = BackoffMs[attempt];
            yield return new RetryMessage
            {
                Attempt = attempt + 1,
                MaxRetries = MaxRetries,
                DelayMs = delayMs,
                Reason = retryReason!,
            };

            await Task.Delay(delayMs, context.CancellationToken);
        }

        sw.Stop();
        _logger.LogInformation("LLM response: {Count} chunks in {ElapsedMs}ms", responseCount, sw.ElapsedMilliseconds);
    }

    private static bool IsTransient(Exception ex)
    {
        return ex is AnthropicRateLimitException
            or Anthropic5xxException
            or HttpRequestException
            || (ex is AnthropicSseException sse && ContainsTransientError(sse.Message));
    }

    private static bool ContainsTransientError(string? message)
    {
        if (message is null) return false;
        return message.Contains("overloaded", StringComparison.OrdinalIgnoreCase)
            || message.Contains("rate_limit", StringComparison.OrdinalIgnoreCase)
            || message.Contains("internal_error", StringComparison.OrdinalIgnoreCase);
    }

    private static string ExtractRetryReason(Exception ex)
    {
        return ex switch
        {
            AnthropicRateLimitException => "Rate limited",
            Anthropic5xxException => "Server error",
            AnthropicSseException sse => sse.Message.Contains("overloaded", StringComparison.OrdinalIgnoreCase)
                ? "API overloaded"
                : "Stream error",
            HttpRequestException => "Network error",
            _ => "Transient error",
        };
    }
}
