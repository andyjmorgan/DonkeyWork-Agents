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
    /// The content parts of the message.
    /// </summary>
    public required IReadOnlyList<ChatContentPart> Content { get; set; }

    /// <summary>
    /// Optional name for the message sender.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Creates a ChatMessage with a single text content part.
    /// </summary>
    public static ChatMessage FromText(ChatMessageRole role, string text, string? name = null)
    {
        return new ChatMessage
        {
            Role = role,
            Content = [new TextChatContentPart { Text = text }],
            Name = name
        };
    }

    /// <summary>
    /// Gets the text content from all text parts concatenated.
    /// </summary>
    public string GetTextContent()
    {
        return string.Join("", Content.OfType<TextChatContentPart>().Select(p => p.Text));
    }
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
