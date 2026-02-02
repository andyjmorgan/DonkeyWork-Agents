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
    /// Number of input tokens used (for assistant messages).
    /// </summary>
    [JsonPropertyName("inputTokens")]
    public int? InputTokens { get; init; }

    /// <summary>
    /// Number of output tokens used (for assistant messages).
    /// </summary>
    [JsonPropertyName("outputTokens")]
    public int? OutputTokens { get; init; }

    /// <summary>
    /// Total tokens used (for assistant messages).
    /// </summary>
    [JsonPropertyName("totalTokens")]
    public int? TotalTokens { get; init; }

    /// <summary>
    /// LLM provider used (for assistant messages).
    /// </summary>
    [JsonPropertyName("provider")]
    public string? Provider { get; init; }

    /// <summary>
    /// Model used (for assistant messages).
    /// </summary>
    [JsonPropertyName("model")]
    public string? Model { get; init; }

    /// <summary>
    /// When the message was created.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; init; }
}
