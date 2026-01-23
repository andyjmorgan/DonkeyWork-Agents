using System.Runtime.CompilerServices;
using DonkeyWork.Agents.Providers.Core.Middleware.Internal;
using DonkeyWork.Agents.Providers.Core.Middleware.Internal.Messages;
using DonkeyWork.Agents.Providers.Core.Middleware.Internal.Responses;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Providers.Core.Middleware;

/// <summary>
/// Middleware that handles tool execution loop.
/// </summary>
internal class ToolMiddleware : IModelMiddleware
{
    private readonly ILogger<ToolMiddleware> _logger;

    public ToolMiddleware(ILogger<ToolMiddleware> logger)
    {
        _logger = logger;
    }

    public async IAsyncEnumerable<BaseMiddlewareMessage> ExecuteAsync(
        ModelMiddlewareContext context,
        Func<ModelMiddlewareContext, IAsyncEnumerable<BaseMiddlewareMessage>> next,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var maxIterations = context.Variables.TryGetValue(MiddlewareVariables.MaxToolExecutions, out var val)
            ? (int)val
            : 20;

        var parallelToolCalls = context.Variables.TryGetValue(MiddlewareVariables.ParallelToolCalls, out var parallelVal)
            && parallelVal is true;

        var currentIteration = 0;
        bool hasToolCalls;

        do
        {
            List<ModelResponseToolCall> toolCalls = [];

            await foreach (var message in next(context).WithCancellation(cancellationToken))
            {
                if (message is ModelMiddlewareMessage { ModelMessage: ModelResponseToolCall toolCall })
                {
                    toolCalls.Add(toolCall);
                    continue;
                }

                if (message is ModelMiddlewareMessage { ModelMessage: ModelResponseServerToolCall })
                {
                    yield return message;
                    continue;
                }

                yield return message;
            }

            hasToolCalls = toolCalls.Count > 0;
            if (hasToolCalls)
            {
                await foreach (var message in HandleToolCallsAsync(toolCalls, cancellationToken))
                {
                    yield return message;
                }
            }

            if (++currentIteration >= maxIterations)
            {
                _logger.LogWarning("Max iterations ({MaxIterations}) reached.", maxIterations);
                yield break;
            }
        }
        while (hasToolCalls);
    }

    private static async IAsyncEnumerable<BaseMiddlewareMessage> HandleToolCallsAsync(
        List<ModelResponseToolCall> toolCalls,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var toolCall in toolCalls)
        {
            yield return new ToolRequestMessage
            {
                CallId = toolCall.CallId,
                ToolName = toolCall.ToolName,
                Arguments = toolCall.Arguments
            };

            // TODO: Execute tool
            yield return new ToolResponseMessage
            {
                CallId = toolCall.CallId,
                ToolName = toolCall.ToolName,
                Response = "{}",
                Success = true
            };
        }

        await Task.CompletedTask;
    }

    IAsyncEnumerable<BaseMiddlewareMessage> IModelMiddleware.ExecuteAsync(
        ModelMiddlewareContext context,
        Func<ModelMiddlewareContext, IAsyncEnumerable<BaseMiddlewareMessage>> next)
    {
        return ExecuteAsync(context, next);
    }
}
