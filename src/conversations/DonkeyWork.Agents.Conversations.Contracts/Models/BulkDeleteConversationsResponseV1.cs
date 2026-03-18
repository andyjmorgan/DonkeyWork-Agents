namespace DonkeyWork.Agents.Conversations.Contracts.Models;

/// <summary>
/// Response from bulk deleting conversations.
/// </summary>
public sealed class BulkDeleteConversationsResponseV1
{
    /// <summary>
    /// The number of conversations that were deleted.
    /// </summary>
    public int DeletedCount { get; init; }
}
