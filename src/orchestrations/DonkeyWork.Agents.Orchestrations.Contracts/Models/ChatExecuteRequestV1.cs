using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Orchestrations.Contracts.Models;

/// <summary>
/// Request to execute an orchestration in chat mode.
/// </summary>
public sealed class ChatExecuteRequestV1
{
    /// <summary>
    /// The conversation message history (user and assistant messages).
    /// </summary>
    [JsonPropertyName("messages")]
    public required IReadOnlyList<ChatMessageV1> Messages { get; init; }

    /// <summary>
    /// Optional version override. If not specified, uses latest published (for execute) or draft (for test).
    /// </summary>
    [JsonPropertyName("versionId")]
    public Guid? VersionId { get; init; }
}

/// <summary>
/// A message in the chat request.
/// </summary>
public sealed class ChatMessageV1
{
    /// <summary>
    /// The role of the message sender (user or assistant).
    /// </summary>
    [JsonPropertyName("role")]
    public required string Role { get; init; }

    /// <summary>
    /// The message content.
    /// </summary>
    [JsonPropertyName("content")]
    public required string Content { get; init; }
}
