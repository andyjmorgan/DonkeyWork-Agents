using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Conversations.Contracts.Models.Events;

/// <summary>
/// Sent when an error occurs during response generation.
/// </summary>
public sealed class ResponseErrorEvent : ConversationStreamEvent
{
    /// <summary>
    /// Error message.
    /// </summary>
    [JsonPropertyName("error")]
    public required string Error { get; init; }
}
