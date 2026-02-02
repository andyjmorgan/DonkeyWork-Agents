using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Enums;

namespace DonkeyWork.Agents.Orchestrations.Contracts.Services;

/// <summary>
/// Registry for node executors.
/// Maps node types to their executor implementations.
/// </summary>
public interface INodeExecutorRegistry
{
    /// <summary>
    /// Gets the executor for a given node type.
    /// </summary>
    /// <param name="nodeType">The node type enum value.</param>
    /// <returns>The executor instance.</returns>
    /// <exception cref="InvalidOperationException">If no executor is registered for the node type.</exception>
    object GetExecutor(NodeType nodeType);
}
