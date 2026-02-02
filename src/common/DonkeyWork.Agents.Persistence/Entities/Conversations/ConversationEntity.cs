using DonkeyWork.Agents.Persistence.Entities.Orchestrations;

namespace DonkeyWork.Agents.Persistence.Entities.Conversations;

/// <summary>
/// Represents a conversation with an orchestration.
/// </summary>
public class ConversationEntity : BaseEntity
{
    /// <summary>
    /// The orchestration this conversation is with.
    /// </summary>
    public Guid OrchestrationId { get; set; }

    /// <summary>
    /// Conversation title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Navigation property to the orchestration.
    /// </summary>
    public OrchestrationEntity? Orchestration { get; set; }

    /// <summary>
    /// Navigation property to conversation messages.
    /// </summary>
    public ICollection<ConversationMessageEntity> Messages { get; set; } = new List<ConversationMessageEntity>();
}
