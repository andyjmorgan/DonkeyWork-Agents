using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Orchestrations.Contracts.Models;

/// <summary>
/// Conversation context for Chat mode execution.
/// Contains the conversation history and current user input.
/// </summary>
public sealed class ConversationContext
{
    /// <summary>
    /// The conversation ID.
    /// </summary>
    [JsonPropertyName("id")]
    public required Guid Id { get; init; }

    /// <summary>
    /// The conversation message history.
    /// </summary>
    [JsonPropertyName("messages")]
    public required IReadOnlyList<ConversationMessage> Messages { get; init; }

    /// <summary>
    /// The current user message being processed.
    /// </summary>
    [JsonPropertyName("currentMessage")]
    public required string CurrentMessage { get; init; }
}

/// <summary>
/// A message in the conversation history.
/// </summary>
public sealed class ConversationMessage
{
    /// <summary>
    /// The role of the message sender.
    /// </summary>
    [JsonPropertyName("role")]
    public required ConversationRole Role { get; init; }

    /// <summary>
    /// The message content.
    /// </summary>
    [JsonPropertyName("content")]
    public required string Content { get; init; }
}

/// <summary>
/// Role in a conversation.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ConversationRole
{
    /// <summary>
    /// User message.
    /// </summary>
    User,

    /// <summary>
    /// Assistant message.
    /// </summary>
    Assistant
}
