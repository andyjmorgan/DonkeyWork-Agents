namespace DonkeyWork.Agents.Agents.Contracts.Services;

/// <summary>
/// Registry for node executors.
/// Maps node types to their executor implementations.
/// </summary>
public interface INodeExecutorRegistry
{
    /// <summary>
    /// Gets the executor for a given node type.
    /// </summary>
    /// <param name="nodeType">The node type string (e.g., "start", "model", "end").</param>
    /// <returns>The executor instance.</returns>
    /// <exception cref="InvalidOperationException">If no executor is registered for the node type.</exception>
    object GetExecutor(string nodeType);
}
