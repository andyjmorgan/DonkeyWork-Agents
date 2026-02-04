using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Conversations.Contracts.Models.Events;

/// <summary>
/// Sent when a content part is complete.
/// </summary>
public sealed class PartEndEvent : ConversationStreamEvent
{
    /// <summary>
    /// Index of the content part that ended.
    /// </summary>
    [JsonPropertyName("partIndex")]
    public required int PartIndex { get; init; }
}
