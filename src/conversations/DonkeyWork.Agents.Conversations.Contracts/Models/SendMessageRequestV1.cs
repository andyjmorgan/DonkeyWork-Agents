using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Conversations.Contracts.Models;

/// <summary>
/// Request to send a message in a conversation.
/// </summary>
public sealed class SendMessageRequestV1
{
    /// <summary>
    /// The message content parts.
    /// </summary>
    [JsonPropertyName("content")]
    [Required]
    [MinLength(1)]
    public required List<ContentPart> Content { get; init; }
}
