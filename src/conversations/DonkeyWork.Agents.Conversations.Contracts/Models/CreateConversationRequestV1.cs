using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Conversations.Contracts.Models;

/// <summary>
/// Request to create a new conversation.
/// </summary>
public sealed class CreateConversationRequestV1
{
    /// <summary>
    /// The orchestration ID to chat with.
    /// </summary>
    [JsonPropertyName("orchestrationId")]
    [Required]
    public required Guid OrchestrationId { get; init; }

    /// <summary>
    /// Optional conversation title. If not provided, a default title will be generated.
    /// </summary>
    [JsonPropertyName("title")]
    [StringLength(255)]
    public string? Title { get; init; }
}
