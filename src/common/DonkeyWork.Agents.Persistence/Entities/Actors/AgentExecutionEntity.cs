namespace DonkeyWork.Agents.Persistence.Entities.Actors;

/// <summary>
/// Tracks a single agent execution within a conversation — conversation agents,
/// delegates, and custom agents. Stores timing, token usage, and a snapshot
/// of the contract for resurrection.
/// </summary>
public class AgentExecutionEntity : BaseEntity
{
    /// <summary>
    /// Parent conversation this execution belongs to.
    /// </summary>
    public Guid ConversationId { get; set; }

    /// <summary>
    /// Agent type: "conversation", "delegate", or "agent".
    /// </summary>
    public string AgentType { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable name for this execution.
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Grain key linking to grain_messages for full message history.
    /// </summary>
    public string GrainKey { get; set; } = string.Empty;

    /// <summary>
    /// Grain key of the agent that spawned this one (null for conversation agents).
    /// </summary>
    public string? ParentGrainKey { get; set; }

    /// <summary>
    /// Serialized AgentContract JSON enabling resurrection.
    /// </summary>
    public string ContractSnapshot { get; set; } = string.Empty;

    /// <summary>
    /// Task text / user input (null for conversation agents).
    /// </summary>
    public string? Input { get; set; }

    /// <summary>
    /// Serialized AgentResult JSON.
    /// </summary>
    public string? Output { get; set; }

    /// <summary>
    /// Running, Completed, Failed, or Cancelled.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Error details when Status is Failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// When this execution started.
    /// </summary>
    public DateTimeOffset StartedAt { get; set; }

    /// <summary>
    /// When this execution completed (null if still running).
    /// </summary>
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>
    /// Total execution duration in milliseconds.
    /// </summary>
    public long? DurationMs { get; set; }

    /// <summary>
    /// Cumulative input tokens used across all LLM calls.
    /// </summary>
    public int? InputTokensUsed { get; set; }

    /// <summary>
    /// Cumulative output tokens used across all LLM calls.
    /// </summary>
    public int? OutputTokensUsed { get; set; }

    /// <summary>
    /// Model identifier used for this execution.
    /// </summary>
    public string? ModelId { get; set; }
}
