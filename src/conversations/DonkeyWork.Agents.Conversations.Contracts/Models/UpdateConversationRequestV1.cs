using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Conversations.Contracts.Models;

/// <summary>
/// Request to update a conversation (rename title).
/// </summary>
public sealed class UpdateConversationRequestV1
{
    /// <summary>
    /// The new conversation title.
    /// </summary>
    [JsonPropertyName("title")]
    [Required]
    [StringLength(255, MinimumLength = 1)]
    public required string Title { get; init; }
}
