using Asp.Versioning;
using DonkeyWork.Agents.Actors.Contracts.Models;
using DonkeyWork.Agents.Actors.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DonkeyWork.Agents.Actors.Api.Controllers;

/// <summary>
/// Endpoints for querying the agent execution audit trail.
/// </summary>
[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/agent-executions")]
[Authorize]
[Produces("application/json")]
public class AgentExecutionsController : ControllerBase
{
    private readonly IAgentExecutionService _executionService;

    public AgentExecutionsController(IAgentExecutionService executionService)
    {
        _executionService = executionService;
    }

    /// <summary>
    /// Lists all agent executions for a conversation, ordered by start time.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<AgentExecutionSummaryV1>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ListByConversation(
        [FromQuery] Guid conversationId,
        CancellationToken ct)
    {
        if (conversationId == Guid.Empty)
            return BadRequest("conversationId is required.");

        var executions = await _executionService.ListByConversationAsync(conversationId, ct);
        return Ok(executions);
    }

    /// <summary>
    /// Gets the full detail of a specific agent execution.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<AgentExecutionDetailV1>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var execution = await _executionService.GetByIdAsync(id, ct);
        if (execution is null)
            return NotFound();

        return Ok(execution);
    }

    /// <summary>
    /// Gets the message history for an agent execution.
    /// </summary>
    [HttpGet("{id:guid}/messages")]
    [ProducesResponseType<GetAgentExecutionMessagesResponseV1>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMessages(Guid id, CancellationToken ct)
    {
        var response = await _executionService.GetMessagesAsync(id, ct);
        if (response is null)
            return NotFound();

        return Ok(response);
    }
}
