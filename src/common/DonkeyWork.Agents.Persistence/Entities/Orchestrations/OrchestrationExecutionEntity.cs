using DonkeyWork.Agents.Orchestrations.Contracts.Enums;

namespace DonkeyWork.Agents.Persistence.Entities.Orchestrations;

/// <summary>
/// Represents an orchestration execution instance.
/// </summary>
public class OrchestrationExecutionEntity : BaseEntity
{
    /// <summary>
    /// Foreign key to the orchestration.
    /// </summary>
    public Guid OrchestrationId { get; set; }

    /// <summary>
    /// Foreign key to the specific version that was executed.
    /// </summary>
    public Guid OrchestrationVersionId { get; set; }

    /// <summary>
    /// The interface through which this execution was triggered.
    /// </summary>
    public ExecutionInterface Interface { get; set; } = ExecutionInterface.Direct;

    /// <summary>
    /// Execution status.
    /// </summary>
    public ExecutionStatus Status { get; set; } = ExecutionStatus.Pending;

    /// <summary>
    /// Input data. Stored as JSONB.
    /// </summary>
    public string Input { get; set; } = string.Empty;

    /// <summary>
    /// Output data. Stored as JSONB.
    /// </summary>
    public string? Output { get; set; }

    /// <summary>
    /// Error message if execution failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Timestamp when execution started.
    /// </summary>
    public DateTimeOffset StartedAt { get; set; }

    /// <summary>
    /// Timestamp when execution completed.
    /// </summary>
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>
    /// Total tokens used across all model nodes.
    /// </summary>
    public int? TotalTokensUsed { get; set; }

    /// <summary>
    /// NATS subject name for this execution.
    /// </summary>
    public string StreamName { get; set; } = string.Empty;

    /// <summary>
    /// Navigation property to the orchestration.
    /// </summary>
    public OrchestrationEntity Orchestration { get; set; } = null!;

    /// <summary>
    /// Navigation property to the orchestration version.
    /// </summary>
    public OrchestrationVersionEntity OrchestrationVersion { get; set; } = null!;

    /// <summary>
    /// Navigation property to node executions.
    /// </summary>
    public ICollection<OrchestrationNodeExecutionEntity> NodeExecutions { get; set; } = new List<OrchestrationNodeExecutionEntity>();
}
