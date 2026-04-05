using Asp.Versioning;
using DonkeyWork.Agents.Common.Contracts.Models.Pagination;
using DonkeyWork.Agents.Mcp.Contracts.Models;
using DonkeyWork.Agents.Mcp.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DonkeyWork.Agents.Mcp.Api.Controllers;

/// <summary>
/// Endpoints for querying the MCP JSON-RPC traffic trace log.
/// </summary>
[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/mcp-traces")]
[Authorize]
[Produces("application/json")]
public class McpTracesController : ControllerBase
{
    private readonly IMcpTraceService _traceService;

    public McpTracesController(IMcpTraceService traceService)
    {
        _traceService = traceService;
    }

    /// <summary>
    /// Lists MCP traces for the current user with pagination, ordered by most recent first.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<PaginatedResponse<McpTraceSummaryV1>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> List([FromQuery] PaginationRequest pagination, CancellationToken ct)
    {
        var offset = Math.Max(0, pagination.Offset);
        var limit = Math.Max(1, pagination.GetLimit());
        var result = await _traceService.ListAsync(offset, limit, ct);
        return Ok(result);
    }

    /// <summary>
    /// Gets the full detail of a specific MCP trace including request and response payloads.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<McpTraceDetailV1>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var trace = await _traceService.GetByIdAsync(id, ct);
        if (trace is null)
            return NotFound();

        return Ok(trace);
    }
}
