using Asp.Versioning;
using DonkeyWork.Agents.Common.Contracts.Models.Pagination;
using DonkeyWork.Agents.Conversations.Contracts.Models;
using DonkeyWork.Agents.Conversations.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Conversations.Api.Controllers;

/// <summary>
/// Manage conversations with orchestrations.
/// </summary>
[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/conversations")]
[Authorize]
[Produces("application/json")]
public class ConversationsController : ControllerBase
{
    private readonly IConversationService _conversationService;
    private readonly ILogger<ConversationsController> _logger;

    public ConversationsController(IConversationService conversationService, ILogger<ConversationsController> logger)
    {
        _conversationService = conversationService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new conversation.
    /// </summary>
    /// <param name="request">The conversation details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="201">Returns the created conversation.</response>
    /// <response code="400">Invalid request or orchestration not found.</response>
    [HttpPost]
    [ProducesResponseType<ConversationDetailsV1>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateConversationRequestV1 request, CancellationToken cancellationToken)
    {
        try
        {
            var conversation = await _conversationService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(Get), new { id = conversation.Id }, conversation);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when creating conversation for orchestration {OrchestrationId}", request.OrchestrationId);
            return BadRequest(new { error = ex.Message });
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error when creating conversation for orchestration {OrchestrationId}", request.OrchestrationId);
            return BadRequest(new { error = "Failed to create conversation. Please verify the orchestration exists and try again." });
        }
    }

    /// <summary>
    /// Get a conversation by ID with all messages.
    /// </summary>
    /// <param name="id">The conversation ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Returns the conversation with messages.</response>
    /// <response code="404">Conversation not found.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<ConversationDetailsV1>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var conversation = await _conversationService.GetByIdAsync(id, cancellationToken);

        if (conversation == null)
            return NotFound();

        return Ok(conversation);
    }

    /// <summary>
    /// List all conversations for the current user (paginated, newest first).
    /// </summary>
    /// <param name="pagination">Pagination parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Returns the list of conversations.</response>
    [HttpGet]
    [ProducesResponseType<PaginatedResponse<ConversationSummaryV1>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] PaginationRequest pagination, CancellationToken cancellationToken)
    {
        var conversations = await _conversationService.ListAsync(pagination, cancellationToken);
        return Ok(conversations);
    }

    /// <summary>
    /// Update a conversation title.
    /// </summary>
    /// <param name="id">The conversation ID.</param>
    /// <param name="request">The update request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Returns the updated conversation.</response>
    /// <response code="404">Conversation not found.</response>
    /// <response code="400">Invalid request.</response>
    [HttpPatch("{id:guid}")]
    [ProducesResponseType<ConversationDetailsV1>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateTitle(Guid id, [FromBody] UpdateConversationRequestV1 request, CancellationToken cancellationToken)
    {
        var conversation = await _conversationService.UpdateTitleAsync(id, request, cancellationToken);

        if (conversation == null)
            return NotFound();

        return Ok(conversation);
    }

    /// <summary>
    /// Delete a conversation and all its messages.
    /// </summary>
    /// <param name="id">The conversation ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="204">Conversation deleted.</response>
    /// <response code="404">Conversation not found.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _conversationService.DeleteAsync(id, cancellationToken);

        if (!deleted)
            return NotFound();

        return NoContent();
    }

    /// <summary>
    /// Send a message in a conversation.
    /// Saves the user message and returns it. Conversation execution is handled via WebSocket.
    /// </summary>
    /// <param name="id">The conversation ID.</param>
    /// <param name="request">The message request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="201">Returns the created user message.</response>
    /// <response code="404">Conversation not found.</response>
    /// <response code="400">Invalid request.</response>
    [HttpPost("{id:guid}/messages")]
    [ProducesResponseType<ConversationMessageV1>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SendMessage(Guid id, [FromBody] SendMessageRequestV1 request, CancellationToken cancellationToken)
    {
        var message = await _conversationService.SendMessageAsync(id, request, cancellationToken);

        if (message == null)
            return NotFound();

        return Created($"/api/v1/conversations/{id}/messages/{message.Id}", message);
    }

    /// <summary>
    /// Delete an individual message from a conversation.
    /// </summary>
    /// <param name="id">The conversation ID.</param>
    /// <param name="messageId">The message ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="204">Message deleted.</response>
    /// <response code="404">Message or conversation not found.</response>
    [HttpDelete("{id:guid}/messages/{messageId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteMessage(Guid id, Guid messageId, CancellationToken cancellationToken)
    {
        var deleted = await _conversationService.DeleteMessageAsync(id, messageId, cancellationToken);

        if (!deleted)
            return NotFound();

        return NoContent();
    }

    /// <summary>
    /// Upload an image for use in conversation messages.
    /// </summary>
    /// <param name="id">The conversation ID.</param>
    /// <param name="file">The image file to upload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="201">Returns the uploaded file details.</response>
    /// <response code="400">Invalid file or validation failed.</response>
    /// <response code="404">Conversation not found.</response>
    [HttpPost("{id:guid}/upload")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10MB
    [ProducesResponseType<UploadImageResponseV1>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadImage(Guid id, IFormFile file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { error = "No file provided or file is empty." });
        }

        try
        {
            await using var stream = file.OpenReadStream();
            var result = await _conversationService.UploadImageAsync(
                id,
                file.FileName,
                file.ContentType,
                stream,
                cancellationToken);

            if (result == null)
                return NotFound();

            return Created($"/api/v1/files/{result.FileId}", result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Image upload validation failed for conversation {ConversationId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }
}
