using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Conversations.Contracts.Models.Events;

/// <summary>
/// Sent when response generation is complete.
/// </summary>
public sealed class ResponseEndEvent : ConversationStreamEvent
{
    /// <summary>
    /// The final assistant message.
    /// </summary>
    [JsonPropertyName("message")]
    public required ConversationMessageV1 Message { get; init; }
}
