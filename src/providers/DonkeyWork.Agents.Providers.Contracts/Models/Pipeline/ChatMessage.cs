namespace DonkeyWork.Agents.Providers.Contracts.Models.Pipeline;

/// <summary>
/// A message in a conversation.
/// </summary>
public class ChatMessage
{
    /// <summary>
    /// The role of the message sender.
    /// </summary>
    public required ChatMessageRole Role { get; set; }

    /// <summary>
    /// The content of the message.
    /// </summary>
    public required string Content { get; set; }

    /// <summary>
    /// Optional name for the message sender.
    /// </summary>
    public string? Name { get; set; }
}

/// <summary>
/// Message roles in a conversation.
/// </summary>
public enum ChatMessageRole
{
    System,
    User,
    Assistant
}
