using System.Text.Json;
using Asp.Versioning;
using DonkeyWork.Agents.Common.Contracts.Models.Pagination;
using DonkeyWork.Agents.Conversations.Contracts.Models;
using DonkeyWork.Agents.Conversations.Contracts.Models.Events;
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
    private readonly JsonSerializerOptions _jsonOptions;

    public ConversationsController(IConversationService conversationService, ILogger<ConversationsController> logger)
    {
        _conversationService = conversationService;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
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
    /// Supports both streaming (Accept: text/event-stream) and non-streaming responses.
    /// Streaming: Executes the orchestration and streams token deltas via SSE.
    /// Non-streaming: Saves the user message and returns it (assistant response saved asynchronously).
    /// </summary>
    /// <param name="id">The conversation ID.</param>
    /// <param name="request">The message request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Returns SSE stream with response events for streaming requests.</response>
    /// <response code="201">Returns the created user message for non-streaming requests.</response>
    /// <response code="404">Conversation not found.</response>
    /// <response code="400">Invalid request.</response>
    [HttpPost("{id:guid}/messages")]
    [ProducesResponseType<ConversationMessageV1>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SendMessage(Guid id, [FromBody] SendMessageRequestV1 request, CancellationToken cancellationToken)
    {
        // Check Accept header for streaming vs non-streaming
        var acceptHeader = Request.Headers["Accept"].ToString();
        if (acceptHeader.Contains("text/event-stream"))
        {
            return await SendMessageStreamingAsync(id, request);
        }

        // Non-streaming: just save the user message
        var message = await _conversationService.SendMessageAsync(id, request, cancellationToken);

        if (message == null)
            return NotFound();

        return Created($"/api/v1/conversations/{id}/messages/{message.Id}", message);
    }

    private async Task<IActionResult> SendMessageStreamingAsync(Guid id, SendMessageRequestV1 request)
    {
        Response.Headers["Content-Type"] = "text/event-stream";
        Response.Headers["Cache-Control"] = "no-cache";
        Response.Headers["Connection"] = "keep-alive";

        var events = _conversationService.SendMessageStreamingAsync(id, request, HttpContext.RequestAborted);

        if (events == null)
        {
            // Reset headers and return 404
            Response.Headers.Remove("Content-Type");
            Response.Headers.Remove("Cache-Control");
            Response.Headers.Remove("Connection");
            return NotFound();
        }

        await foreach (var evt in events)
        {
            var json = JsonSerializer.Serialize<ConversationStreamEvent>(evt, _jsonOptions);
            await Response.WriteAsync($"data: {json}\n\n");
            await Response.Body.FlushAsync();

            // Stop on terminal events
            if (evt is ResponseEndEvent or ResponseErrorEvent)
                break;
        }

        return new EmptyResult();
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
}
