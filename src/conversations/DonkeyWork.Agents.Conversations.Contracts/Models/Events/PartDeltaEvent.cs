using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Conversations.Contracts.Models.Events;

/// <summary>
/// Sent when new content is generated for a part.
/// </summary>
public sealed class PartDeltaEvent : ConversationStreamEvent
{
    /// <inheritdoc />
    [JsonPropertyName("type")]
    public override string Type => "part_delta";

    /// <summary>
    /// Index of the content part being updated.
    /// </summary>
    [JsonPropertyName("partIndex")]
    public required int PartIndex { get; init; }

    /// <summary>
    /// The new content delta.
    /// </summary>
    [JsonPropertyName("content")]
    public required string Content { get; init; }
}
