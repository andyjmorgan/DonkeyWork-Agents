using DonkeyWork.Agents.Common.Contracts.Models.Pagination;
using DonkeyWork.Agents.Conversations.Contracts.Models;

namespace DonkeyWork.Agents.Conversations.Contracts.Services;

/// <summary>
/// Service for managing conversations.
/// </summary>
public interface IConversationService
{
    /// <summary>
    /// Creates a new conversation.
    /// </summary>
    /// <param name="request">The create request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created conversation details.</returns>
    Task<ConversationDetailsV1> CreateAsync(CreateConversationRequestV1 request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a conversation by ID with all messages.
    /// </summary>
    /// <param name="conversationId">The conversation ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The conversation details or null if not found.</returns>
    Task<ConversationDetailsV1?> GetByIdAsync(Guid conversationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists conversations for the current user (paginated, newest first).
    /// </summary>
    /// <param name="pagination">Pagination parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated list of conversation summaries.</returns>
    Task<PaginatedResponse<ConversationSummaryV1>> ListAsync(PaginationRequest pagination, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a conversation title.
    /// </summary>
    /// <param name="conversationId">The conversation ID.</param>
    /// <param name="request">The update request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated conversation details or null if not found.</returns>
    Task<ConversationDetailsV1?> UpdateTitleAsync(Guid conversationId, UpdateConversationRequestV1 request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a conversation and all its messages.
    /// </summary>
    /// <param name="conversationId">The conversation ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted, false if not found.</returns>
    Task<bool> DeleteAsync(Guid conversationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a message to a conversation (non-streaming).
    /// Saves the user message, executes the orchestration, and saves the assistant response.
    /// </summary>
    /// <param name="conversationId">The conversation ID.</param>
    /// <param name="request">The message request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created user message or null if conversation not found.</returns>
    Task<ConversationMessageV1?> SendMessageAsync(Guid conversationId, SendMessageRequestV1 request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an individual message from a conversation.
    /// </summary>
    /// <param name="conversationId">The conversation ID.</param>
    /// <param name="messageId">The message ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted, false if not found.</returns>
    Task<bool> DeleteMessageAsync(Guid conversationId, Guid messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads an image for use in conversation messages.
    /// </summary>
    /// <param name="conversationId">The conversation ID.</param>
    /// <param name="fileName">The original file name.</param>
    /// <param name="contentType">The content type of the file.</param>
    /// <param name="fileStream">The file content stream.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The upload result, or null if conversation not found.</returns>
    Task<UploadImageResponseV1?> UploadImageAsync(Guid conversationId, string fileName, string contentType, Stream fileStream, CancellationToken cancellationToken = default);
}
