using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Conversations.Contracts.Models.Events;

/// <summary>
/// Sent at the beginning of assistant response generation.
/// </summary>
public sealed class ResponseStartEvent : ConversationStreamEvent
{
    /// <summary>
    /// The message ID being generated.
    /// </summary>
    [JsonPropertyName("messageId")]
    public required Guid MessageId { get; init; }
}
