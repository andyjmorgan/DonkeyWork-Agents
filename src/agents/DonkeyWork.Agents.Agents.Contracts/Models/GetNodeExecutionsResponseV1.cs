namespace DonkeyWork.Agents.Agents.Contracts.Models;

/// <summary>
/// Response containing node executions for an agent execution.
/// </summary>
public class GetNodeExecutionsResponseV1
{
    /// <summary>
    /// List of node executions in order.
    /// </summary>
    public IReadOnlyList<NodeExecutionV1> NodeExecutions { get; set; } = Array.Empty<NodeExecutionV1>();

    /// <summary>
    /// Total number of node executions (for pagination).
    /// </summary>
    public int TotalCount { get; set; }
}
