using System.ComponentModel.DataAnnotations;

namespace DonkeyWork.Agents.Conversations.Contracts.Models;

/// <summary>
/// Request to bulk delete conversations.
/// </summary>
public sealed class BulkDeleteConversationsRequestV1
{
    /// <summary>
    /// The IDs of conversations to delete.
    /// </summary>
    [Required]
    [MinLength(1)]
    [MaxLength(100)]
    public required List<Guid> Ids { get; init; }
}
