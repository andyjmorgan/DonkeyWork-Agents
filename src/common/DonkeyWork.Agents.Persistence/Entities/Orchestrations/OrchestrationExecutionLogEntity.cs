namespace DonkeyWork.Agents.Persistence.Entities.Orchestrations;

/// <summary>
/// Represents a log entry for an orchestration execution.
/// </summary>
public class OrchestrationExecutionLogEntity : BaseEntity
{
    /// <summary>
    /// Foreign key to the execution.
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
    /// Optional structured details (stored as JSONB).
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// Node ID if this log is specific to a node execution.
    /// </summary>
    public string? NodeId { get; set; }

    /// <summary>
    /// Source/category of the log (e.g., "Executor", "NodeExecutor", "Provider").
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Navigation property to the execution.
    /// </summary>
    public OrchestrationExecutionEntity Execution { get; set; } = null!;
}
