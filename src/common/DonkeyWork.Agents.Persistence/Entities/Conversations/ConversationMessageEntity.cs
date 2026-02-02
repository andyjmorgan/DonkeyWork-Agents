namespace DonkeyWork.Agents.Persistence.Entities.Conversations;

/// <summary>
/// Represents a message in a conversation.
/// </summary>
public class ConversationMessageEntity : BaseEntity
{
    /// <summary>
    /// The conversation this message belongs to.
    /// </summary>
    public Guid ConversationId { get; set; }

    /// <summary>
    /// Message role (User, Assistant, System).
    /// </summary>
    public MessageRole Role { get; set; }

    /// <summary>
    /// Message content as JSON array of ContentPart objects.
    /// Stored as JSONB in PostgreSQL.
    /// </summary>
    public string Content { get; set; } = "[]";

    /// <summary>
    /// Number of input tokens used (for assistant messages).
    /// </summary>
    public int? InputTokens { get; set; }

    /// <summary>
    /// Number of output tokens used (for assistant messages).
    /// </summary>
    public int? OutputTokens { get; set; }

    /// <summary>
    /// Total tokens used (for assistant messages).
    /// </summary>
    public int? TotalTokens { get; set; }

    /// <summary>
    /// LLM provider used (for assistant messages).
    /// </summary>
    public string? Provider { get; set; }

    /// <summary>
    /// Model used (for assistant messages).
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    /// Navigation property to the conversation.
    /// </summary>
    public ConversationEntity? Conversation { get; set; }
}
