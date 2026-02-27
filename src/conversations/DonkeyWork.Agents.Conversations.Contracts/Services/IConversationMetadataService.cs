namespace DonkeyWork.Agents.Conversations.Contracts.Services;

/// <summary>
/// Service for managing conversation metadata records from grain context.
/// </summary>
public interface IConversationMetadataService
{
    /// <summary>
    /// Ensures a conversation record exists in the database.
    /// Creates one with OrchestrationId = null if it doesn't exist.
    /// </summary>
    Task EnsureExistsAsync(Guid conversationId, Guid userId, string title, CancellationToken ct = default);

    /// <summary>
    /// Updates the title of a conversation.
    /// </summary>
    Task UpdateTitleAsync(Guid conversationId, Guid userId, string title, CancellationToken ct = default);

    /// <summary>
    /// Updates the UpdatedAt timestamp of a conversation.
    /// </summary>
    Task TouchTimestampAsync(Guid conversationId, Guid userId, CancellationToken ct = default);
}
