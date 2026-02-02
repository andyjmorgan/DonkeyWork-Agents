using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Conversations.Contracts.Models;

/// <summary>
/// Full conversation details including all messages.
/// </summary>
public sealed class ConversationDetailsV1
{
    /// <summary>
    /// Conversation ID.
    /// </summary>
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    /// <summary>
    /// Orchestration ID.
    /// </summary>
    [JsonPropertyName("orchestrationId")]
    public Guid OrchestrationId { get; init; }

    /// <summary>
    /// Orchestration name.
    /// </summary>
    [JsonPropertyName("orchestrationName")]
    public required string OrchestrationName { get; init; }

    /// <summary>
    /// Conversation title.
    /// </summary>
    [JsonPropertyName("title")]
    public required string Title { get; init; }

    /// <summary>
    /// All messages in the conversation, ordered by creation time.
    /// </summary>
    [JsonPropertyName("messages")]
    public List<ConversationMessageV1> Messages { get; init; } = [];

    /// <summary>
    /// When the conversation was created.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// When the conversation was last updated.
    /// </summary>
    [JsonPropertyName("updatedAt")]
    public DateTimeOffset? UpdatedAt { get; init; }
}
