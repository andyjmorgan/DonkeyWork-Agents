using System.Diagnostics;
using System.Text.Json;
using DonkeyWork.Agents.Orchestrations.Contracts.Models.Events;
using DonkeyWork.Agents.Orchestrations.Contracts.Services;

namespace DonkeyWork.Agents.Orchestrations.Core.Execution;

/// <summary>
/// Base class for node executors that handles common orchestration concerns.
/// </summary>
/// <typeparam name="TConfig">The node configuration type.</typeparam>
/// <typeparam name="TOutput">The node output type.</typeparam>
public abstract class NodeExecutor<TConfig, TOutput> : INodeExecutor
    where TOutput : NodeOutput
{
    private readonly IExecutionStreamWriter _streamWriter;
    private readonly IExecutionContext _context;

    protected NodeExecutor(
        IExecutionStreamWriter streamWriter,
        IExecutionContext context)
    {
        _streamWriter = streamWriter;
        _context = context;
    }

    /// <summary>
    /// Gets the execution context.
    /// </summary>
    protected IExecutionContext Context => _context;

    /// <summary>
    /// Executes the node with the given configuration.
    /// Derived classes implement this method with their specific logic.
    /// </summary>
    /// <param name="config">The node configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The node output.</returns>
    protected abstract Task<TOutput> ExecuteInternalAsync(
        TConfig config,
        CancellationToken cancellationToken);

    /// <summary>
    /// Executes the node, handling timing, events, and exceptions.
    /// Emits NodeStarted and NodeCompleted events to the stream.
    /// </summary>
    public async Task<NodeOutput> ExecuteAsync(
        Guid nodeId,
        object config,
        CancellationToken cancellationToken)
    {
        if (config is not TConfig typedConfig)
        {
            throw new InvalidOperationException(
                $"Expected configuration of type {typeof(TConfig).Name} but received {config.GetType().Name}");
        }

        // Emit NodeStarted event
        await _streamWriter.WriteEventAsync(new NodeStartedEvent { NodeId = nodeId });

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Execute the node
            var output = await ExecuteInternalAsync(typedConfig, cancellationToken);
            stopwatch.Stop();

            // Emit NodeCompleted event with output
            await _streamWriter.WriteEventAsync(
                new NodeCompletedEvent
                {
                    NodeId = nodeId,
                    Output = JsonSerializer.Serialize(output)
                });

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
