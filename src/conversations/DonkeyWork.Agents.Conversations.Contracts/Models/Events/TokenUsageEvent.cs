using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Conversations.Contracts.Models.Events;

/// <summary>
/// Sent with token usage information.
/// </summary>
public sealed class TokenUsageEvent : ConversationStreamEvent
{
    /// <summary>
    /// Number of input tokens used.
    /// </summary>
    [JsonPropertyName("inputTokens")]
    public required int InputTokens { get; init; }

    /// <summary>
    /// Number of output tokens generated.
    /// </summary>
    [JsonPropertyName("outputTokens")]
    public required int OutputTokens { get; init; }

    /// <summary>
    /// Total tokens used.
    /// </summary>
    [JsonPropertyName("totalTokens")]
    public required int TotalTokens { get; init; }
}
