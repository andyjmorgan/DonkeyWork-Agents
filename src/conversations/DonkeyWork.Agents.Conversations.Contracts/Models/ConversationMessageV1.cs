using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Conversations.Contracts.Models;

/// <summary>
/// A message in a conversation.
/// </summary>
public sealed class ConversationMessageV1
{
    /// <summary>
    /// Message ID.
    /// </summary>
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    /// <summary>
    /// Message role (User, Assistant, System).
    /// </summary>
    [JsonPropertyName("role")]
    public MessageRole Role { get; init; }

    /// <summary>
    /// Message content parts.
    /// </summary>
    [JsonPropertyName("content")]
    public List<ContentPart> Content { get; init; } = [];

    /// <summary>
    /// When the message was created.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; init; }
}
