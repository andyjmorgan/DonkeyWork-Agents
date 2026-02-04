using DonkeyWork.Agents.Providers.Contracts.Models.Pipeline;

namespace DonkeyWork.Agents.Providers.Core.Middleware.Internal;

/// <summary>
/// Internal base class for conversation messages.
/// </summary>
internal abstract class InternalMessage
{
    public required InternalMessageRole Role { get; set; }
}

/// <summary>
/// Internal message with content parts. Uses the public ChatContentPart directly.
/// </summary>
internal class InternalContentMessage : InternalMessage
{
    public required IReadOnlyList<ChatContentPart> Content { get; set; }

    /// <summary>
    /// Gets the text content from all text parts concatenated.
    /// </summary>
    public string GetTextContent()
    {
        return string.Join("", Content.OfType<TextChatContentPart>().Select(p => p.Text));
    }
}

internal enum InternalMessageRole
{
    System,
    User,
    Assistant
}
