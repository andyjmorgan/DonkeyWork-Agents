using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Conversations.Contracts.Models.Events;

/// <summary>
/// Sent when a new content part begins.
/// </summary>
public sealed class PartStartEvent : ConversationStreamEvent
{
    /// <inheritdoc />
    [JsonPropertyName("type")]
    public override string Type => "part_start";

    /// <summary>
    /// The type of content part (e.g., "text").
    /// </summary>
    [JsonPropertyName("partType")]
    public required string PartType { get; init; }

    /// <summary>
    /// Index of the content part in the response.
    /// </summary>
    [JsonPropertyName("partIndex")]
    public required int PartIndex { get; init; }
}
