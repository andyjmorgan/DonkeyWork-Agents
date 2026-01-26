using DonkeyWork.Agents.Agents.Contracts.Services;

namespace DonkeyWork.Agents.Agents.Core.Execution;

/// <summary>
/// Non-generic interface for node executors (used by registry).
/// </summary>
public interface INodeExecutor
{
    /// <summary>
    /// Executes a node with the given configuration and context.
    /// </summary>
    /// <param name="config">The node configuration.</param>
    /// <param name="context">The execution context.</param>
    /// <param name="streamService">Service for emitting stream events.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The node output.</returns>
    Task<NodeOutput> ExecuteAsync(
        object config,
        ExecutionContext context,
        IExecutionStreamService streamService,
        CancellationToken cancellationToken);
}
