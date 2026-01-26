namespace DonkeyWork.Agents.Agents.Contracts.Enums;

/// <summary>
/// Represents the status of an agent execution.
/// </summary>
public enum ExecutionStatus
{
    /// <summary>
    /// Execution has been created but not started yet.
    /// </summary>
    Pending,

    /// <summary>
    /// Execution is currently running.
    /// </summary>
    Running,

    /// <summary>
    /// Execution completed successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// Execution failed with an error.
    /// </summary>
    Failed,

    /// <summary>
    /// Execution was cancelled.
    /// </summary>
    Cancelled
}
