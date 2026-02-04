using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Conversations.Contracts.Models.Events;

/// <summary>
/// Base class for conversation streaming events.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(ResponseStartEvent), nameof(ResponseStartEvent))]
[JsonDerivedType(typeof(PartStartEvent), nameof(PartStartEvent))]
[JsonDerivedType(typeof(PartDeltaEvent), nameof(PartDeltaEvent))]
[JsonDerivedType(typeof(PartEndEvent), nameof(PartEndEvent))]
[JsonDerivedType(typeof(TokenUsageEvent), nameof(TokenUsageEvent))]
[JsonDerivedType(typeof(ResponseErrorEvent), nameof(ResponseErrorEvent))]
[JsonDerivedType(typeof(ResponseEndEvent), nameof(ResponseEndEvent))]
public abstract class ConversationStreamEvent
{
    /// <summary>
    /// Event timestamp.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
