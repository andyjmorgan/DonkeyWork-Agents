using System.Text.Json.Serialization;
using DonkeyWork.Agents.Providers.Contracts.Models.Pipeline;

namespace DonkeyWork.Agents.Orchestrations.Contracts.Models;

/// <summary>
/// Conversation context for Chat mode execution.
/// Contains the conversation history and current user input.
/// Content parts are hydrated (images converted to base64) before being stored here.
/// </summary>
public sealed class ConversationContext
{
    /// <summary>
    /// The conversation ID.
    /// </summary>
    [JsonPropertyName("id")]
    public required Guid Id { get; init; }

    /// <summary>
    /// The conversation message history with hydrated content.
    /// </summary>
    [JsonPropertyName("messages")]
    public required IReadOnlyList<ConversationMessage> Messages { get; init; }

    /// <summary>
    /// The current user message content parts being processed.
    /// Images are already hydrated to base64.
    /// </summary>
    [JsonPropertyName("currentMessage")]
    public required IReadOnlyList<ChatContentPart> CurrentMessage { get; init; }
}

/// <summary>
/// A message in the conversation history with hydrated content.
/// </summary>
public sealed class ConversationMessage
{
    /// <summary>
    /// The role of the message sender.
    /// </summary>
    [JsonPropertyName("role")]
    public required ConversationRole Role { get; init; }

    /// <summary>
    /// The message content parts. Images are already hydrated to base64.
    /// </summary>
    [JsonPropertyName("content")]
    public required IReadOnlyList<ChatContentPart> Content { get; init; }
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
