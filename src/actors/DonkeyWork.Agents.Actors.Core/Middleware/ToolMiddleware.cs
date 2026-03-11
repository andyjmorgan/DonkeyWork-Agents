using System.Diagnostics;
using DonkeyWork.Agents.Actors.Contracts.Messages;
using DonkeyWork.Agents.Actors.Core.Middleware.Messages;
using DonkeyWork.Agents.Actors.Core.Providers.Responses;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Actors.Core.Middleware;

internal sealed class ToolMiddleware : IModelMiddleware
{
    private const int MaxIterations = 25;
    private const int MaxToolResultCharacters = 20_000;
    private readonly ILogger<ToolMiddleware> _logger;

    public ToolMiddleware(ILogger<ToolMiddleware> logger) => _logger = logger;

    public async IAsyncEnumerable<BaseMiddlewareMessage> ExecuteAsync(
        ModelMiddlewareContext context,
        Func<ModelMiddlewareContext, IAsyncEnumerable<BaseMiddlewareMessage>> next)
    {
        if (context.ToolExecutor is null)
        {
            _logger.LogInformation("ToolMiddleware: no executor, passing through");
            await foreach (var msg in next(context))
                yield return msg;
            yield break;
        }

        for (int iteration = 0; iteration < MaxIterations; iteration++)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            _logger.LogInformation("ToolMiddleware iteration {Iteration}, calling inner pipeline", iteration + 1);

            var eagerTasks = new List<Task<(ModelResponseToolCall toolCall, ToolExecutionResult result, TimeSpan duration)>>();

            // Stream text/thinking immediately; start tool execution eagerly as
            // each tool_use block completes streaming (don't wait for the full response).
            await foreach (var msg in next(context))
            {
                if (msg is ModelMiddlewareMessage { ModelMessage: ModelResponseToolCall tc })
                {
                    yield return msg;
                    eagerTasks.Add(ExecuteToolEagerAsync(tc, context));
                }
                else
                {
                    yield return msg;
                }
            }

            if (eagerTasks.Count == 0)
            {
                _logger.LogInformation("ToolMiddleware: no tool calls, done");
                break;
            }

            _logger.LogInformation("ToolMiddleware: awaiting {ToolCount} eagerly-started tool(s)",
                eagerTasks.Count);

            // Tools were already started during streaming — just wait for stragglers
            var results = await Task.WhenAll(eagerTasks);

            context.CancellationToken.ThrowIfCancellationRequested();

            // Yield responses and add results to context for the next LLM call
            foreach (var (toolCall, result, duration) in results)
            {
                yield return new ToolResponseMessage
                {
                    ToolCallId = toolCall.ToolUseId,
                    ToolName = toolCall.ToolName,
                    Response = result.Content,
                    Duration = duration,
                    Success = !result.IsError
                };

                var toolResultMessage = new InternalToolResultMessage
                {
                    Role = InternalMessageRole.User,
                    ToolUseId = toolCall.ToolUseId,
                    Content = result.Content,
                    IsError = result.IsError,
                    TurnId = context.TurnId,
                    ParentTurnId = context.ParentTurnId,
                };

                context.Messages.Add(toolResultMessage);

                if (context.PersistMessage is not null)
                    await context.PersistMessage(toolResultMessage);
            }

            // Loop back — next iteration calls LLM with tool results appended
        }
    }

    private async Task<(ModelResponseToolCall toolCall, ToolExecutionResult result, TimeSpan duration)> ExecuteToolEagerAsync(
        ModelResponseToolCall toolCall, ModelMiddlewareContext context)
    {
        _logger.LogInformation("ToolMiddleware executing tool {Tool} (id: {Id})", toolCall.ToolName, toolCall.ToolUseId);

        var sw = Stopwatch.StartNew();
        ToolExecutionResult result;
        try
        {
            result = await context.ToolExecutor!.ExecuteAsync(
                toolCall.ToolName, toolCall.Input, context.CancellationToken);
        }
        catch (Exception ex)
        {
            result = new ToolExecutionResult(ex.Message, IsError: true);
        }
        sw.Stop();

        // Guard against oversized tool results that would blow the context window
        if (result.Content.Length > MaxToolResultCharacters)
        {
            _logger.LogWarning(
                "ToolMiddleware tool {Tool} result too large ({Length} chars), replacing with error",
                toolCall.ToolName, result.Content.Length);
            result = new ToolExecutionResult(
                $"Tool result too large ({result.Content.Length:N0} characters, limit is {MaxToolResultCharacters:N0}). " +
                "Try narrowing your request to return less data.",
                IsError: true);
        }

        _logger.LogInformation("ToolMiddleware tool {Tool} completed in {Duration}ms, success={Success}",
            toolCall.ToolName, sw.ElapsedMilliseconds, !result.IsError);

        return (toolCall, result, sw.Elapsed);
    }
}
