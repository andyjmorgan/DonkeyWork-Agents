using System.Diagnostics;
using DonkeyWork.Agents.Agents.Contracts.Models.Events;
using DonkeyWork.Agents.Agents.Contracts.Services;

namespace DonkeyWork.Agents.Agents.Core.Execution;

/// <summary>
/// Base class for node executors that handles common orchestration concerns.
/// </summary>
/// <typeparam name="TConfig">The node configuration type.</typeparam>
/// <typeparam name="TOutput">The node output type.</typeparam>
public abstract class NodeExecutor<TConfig, TOutput> : INodeExecutor
    where TOutput : NodeOutput
{
    /// <summary>
    /// Executes the node with the given configuration.
    /// Derived classes implement this method with their specific logic.
    /// </summary>
    /// <param name="config">The node configuration.</param>
    /// <param name="context">The execution context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The node output.</returns>
    protected abstract Task<TOutput> ExecuteInternalAsync(
        TConfig config,
        ExecutionContext context,
        CancellationToken cancellationToken);

    /// <summary>
    /// Executes the node, handling timing, events, and exceptions.
    /// </summary>
    public async Task<NodeOutput> ExecuteAsync(
        object config,
        ExecutionContext context,
        IExecutionStreamService streamService,
        CancellationToken cancellationToken)
    {
        if (config is not TConfig typedConfig)
        {
            throw new InvalidOperationException(
                $"Expected configuration of type {typeof(TConfig).Name} but received {config.GetType().Name}");
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Execute the node
            var output = await ExecuteInternalAsync(typedConfig, context, cancellationToken);

            stopwatch.Stop();

            return output;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            // Re-throw the exception to be handled by the orchestrator
            throw new InvalidOperationException(
                $"Node execution failed: {ex.Message}", ex);
        }
    }
}
