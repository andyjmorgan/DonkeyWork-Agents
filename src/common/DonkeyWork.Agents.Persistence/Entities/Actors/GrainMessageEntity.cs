namespace DonkeyWork.Agents.Persistence.Entities.Actors;

/// <summary>
/// Persisted grain message — one row per message in a grain's conversation history.
/// The message content is stored as raw JSONB (serialized InternalMessage).
/// </summary>
public class GrainMessageEntity : BaseEntity
{
    /// <summary>
    /// Grain key (e.g. "conv:{userId}:{conversationId}" or "agent:{userId}:{agentKey}").
    /// </summary>
    public string GrainKey { get; set; } = string.Empty;

    /// <summary>
    /// Monotonically increasing sequence number within the grain, used for ordering.
    /// </summary>
    public int SequenceNumber { get; set; }

    /// <summary>
    /// The serialized InternalMessage as raw JSON, stored as JSONB in PostgreSQL.
    /// Deserialization is handled by GrainMessageStore in the Actors.Core layer.
    /// </summary>
    public string MessageJson { get; set; } = string.Empty;
}
