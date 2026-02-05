using DonkeyWork.Agents.Conversations.Contracts.Models;

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
    /// Message content as an array of ContentPart objects.
    /// Stored as JSONB in PostgreSQL.
    /// </summary>
    public List<ContentPart> Content { get; set; } = [];

    /// <summary>
    /// Navigation property to the conversation.
    /// </summary>
    public ConversationEntity? Conversation { get; set; }
}
