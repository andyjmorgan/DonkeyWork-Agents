namespace DonkeyWork.Agents.Agents.Core.Execution;

/// <summary>
/// Non-generic interface for node executors (used by registry).
/// </summary>
public interface INodeExecutor
{
    /// <summary>
    /// Executes a node with the given configuration.
    /// The execution context is injected via DI.
    /// Emits NodeStarted and NodeCompleted events.
    /// </summary>
    /// <param name="nodeId">The node ID.</param>
    /// <param name="config">The node configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The node output.</returns>
    Task<NodeOutput> ExecuteAsync(
        Guid nodeId,
        object config,
        CancellationToken cancellationToken);
}
