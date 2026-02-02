namespace DonkeyWork.Agents.Orchestrations.Contracts.Models;

/// <summary>
/// Represents a log entry for an orchestration execution.
/// </summary>
public class ExecutionLogV1
{
    /// <summary>
    /// Log entry ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Execution ID.
    /// </summary>
    public Guid ExecutionId { get; set; }

    /// <summary>
    /// Log level (Debug, Information, Warning, Error, Critical).
    /// </summary>
    public string LogLevel { get; set; } = string.Empty;

    /// <summary>
    /// Log message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Optional structured details (JSON).
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// Node ID if this log is specific to a node execution.
    /// </summary>
    public string? NodeId { get; set; }

    /// <summary>
    /// Source/category of the log (e.g., "Orchestrator", "NodeExecutor", "Provider").
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the log was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
