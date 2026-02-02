namespace DonkeyWork.Agents.Persistence.Entities.Conversations;

/// <summary>
/// Role of a message in a conversation.
/// </summary>
public enum MessageRole
{
    /// <summary>
    /// Message from the user.
    /// </summary>
    User = 0,

    /// <summary>
    /// Message from the assistant.
    /// </summary>
    Assistant = 1,

    /// <summary>
    /// System message.
    /// </summary>
    System = 2
}
