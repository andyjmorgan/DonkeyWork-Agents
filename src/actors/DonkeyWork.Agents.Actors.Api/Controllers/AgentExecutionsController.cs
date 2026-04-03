using Asp.Versioning;
using DonkeyWork.Agents.Actors.Contracts.Models;
using DonkeyWork.Agents.Actors.Contracts.Services;
using DonkeyWork.Agents.Common.Contracts.Models.Pagination;
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
    /// Lists agent executions. When conversationId is provided, returns executions for that conversation.
    /// Otherwise returns all executions for the current user with pagination.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<PaginatedResponse<AgentExecutionSummaryV1>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> List(
        [FromQuery] Guid? conversationId,
        [FromQuery] PaginationRequest pagination,
        CancellationToken ct)
    {
        if (conversationId.HasValue)
        {
            var executions = await _executionService.ListByConversationAsync(conversationId.Value, ct);
            return Ok(new PaginatedResponse<AgentExecutionSummaryV1>
            {
                Items = executions,
                Offset = 0,
                Limit = executions.Count,
                TotalCount = executions.Count,
            });
        }

        var result = await _executionService.ListAsync(pagination.Offset, pagination.GetLimit(), ct);
        return Ok(result);
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
