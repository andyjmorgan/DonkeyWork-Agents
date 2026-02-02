using DonkeyWork.Agents.Agents.Contracts.Enums;

namespace DonkeyWork.Agents.Persistence.Entities.Agents;

/// <summary>
/// Represents an agent execution instance.
/// </summary>
public class AgentExecutionEntity : BaseEntity
{
    /// <summary>
    /// Foreign key to the agent.
    /// </summary>
    public Guid AgentId { get; set; }

    /// <summary>
    /// Foreign key to the specific version that was executed.
    /// </summary>
    public Guid AgentVersionId { get; set; }

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
    /// RabbitMQ stream name for this execution.
    /// </summary>
    public string StreamName { get; set; } = string.Empty;

    /// <summary>
    /// Navigation property to the agent.
    /// </summary>
    public AgentEntity Agent { get; set; } = null!;

    /// <summary>
    /// Navigation property to the agent version.
    /// </summary>
    public AgentVersionEntity AgentVersion { get; set; } = null!;

    /// <summary>
    /// Navigation property to node executions.
    /// </summary>
    public ICollection<AgentNodeExecutionEntity> NodeExecutions { get; set; } = new List<AgentNodeExecutionEntity>();
}
