using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Conversations.Contracts.Models;

/// <summary>
/// Summary information about a conversation (for list views).
/// </summary>
public sealed class ConversationSummaryV1
{
    /// <summary>
    /// Conversation ID.
    /// </summary>
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    /// <summary>
    /// Orchestration ID (null for agent-only conversations).
    /// </summary>
    [JsonPropertyName("orchestrationId")]
    public Guid? OrchestrationId { get; init; }

    /// <summary>
    /// Orchestration name (null for agent-only conversations).
    /// </summary>
    [JsonPropertyName("orchestrationName")]
    public string? OrchestrationName { get; init; }

    /// <summary>
    /// Conversation title.
    /// </summary>
    [JsonPropertyName("title")]
    public required string Title { get; init; }

    /// <summary>
    /// Number of messages in the conversation.
    /// </summary>
    [JsonPropertyName("messageCount")]
    public int MessageCount { get; init; }

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
