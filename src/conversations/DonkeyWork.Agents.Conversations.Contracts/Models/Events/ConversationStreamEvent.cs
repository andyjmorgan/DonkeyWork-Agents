using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Conversations.Contracts.Models.Events;

/// <summary>
/// Base class for conversation streaming events.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(ResponseStartEvent), "response_start")]
[JsonDerivedType(typeof(PartStartEvent), "part_start")]
[JsonDerivedType(typeof(PartDeltaEvent), "part_delta")]
[JsonDerivedType(typeof(PartEndEvent), "part_end")]
[JsonDerivedType(typeof(TokenUsageEvent), "token_usage")]
[JsonDerivedType(typeof(ResponseErrorEvent), "response_error")]
[JsonDerivedType(typeof(ResponseEndEvent), "response_end")]
public abstract class ConversationStreamEvent
{
    /// <summary>
    /// Event type discriminator.
    /// </summary>
    [JsonPropertyName("type")]
    public abstract string Type { get; }

    /// <summary>
    /// Event timestamp.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
