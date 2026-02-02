using DonkeyWork.Agents.Orchestrations.Contracts.Enums;
using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Enums;

namespace DonkeyWork.Agents.Persistence.Entities.Orchestrations;

/// <summary>
/// Represents a single node execution within an orchestration execution.
/// </summary>
public class OrchestrationNodeExecutionEntity : BaseEntity
{
    /// <summary>
    /// Foreign key to the orchestration execution.
    /// </summary>
    public Guid OrchestrationExecutionId { get; set; }

    /// <summary>
    /// Node ID from the orchestration version configuration.
    /// </summary>
    public Guid NodeId { get; set; }

    /// <summary>
    /// Node type.
    /// </summary>
    public NodeType NodeType { get; set; }

    /// <summary>
    /// Node name from configuration (user-friendly name).
    /// </summary>
    public string NodeName { get; set; } = string.Empty;

    /// <summary>
    /// For action nodes, the specific action type (e.g., "http_request", "sleep").
    /// </summary>
    public string? ActionType { get; set; }

    /// <summary>
    /// Node execution status.
    /// </summary>
    public ExecutionStatus Status { get; set; } = ExecutionStatus.Pending;

    /// <summary>
    /// Input to this node. Stored as JSONB.
    /// </summary>
    public string? Input { get; set; }

    /// <summary>
    /// Output from this node. Stored as JSONB.
    /// </summary>
    public string? Output { get; set; }

    /// <summary>
    /// Error message if node execution failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Timestamp when node execution started.
    /// </summary>
    public DateTimeOffset StartedAt { get; set; }

    /// <summary>
    /// Timestamp when node execution completed.
    /// </summary>
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>
    /// Tokens used for model nodes.
    /// </summary>
    public int? TokensUsed { get; set; }

    /// <summary>
    /// Full LLM response for model nodes.
    /// </summary>
    public string? FullResponse { get; set; }

    /// <summary>
    /// Execution duration in milliseconds.
    /// </summary>
    public int? DurationMs { get; set; }

    /// <summary>
    /// Navigation property to the orchestration execution.
    /// </summary>
    public OrchestrationExecutionEntity OrchestrationExecution { get; set; } = null!;
}
