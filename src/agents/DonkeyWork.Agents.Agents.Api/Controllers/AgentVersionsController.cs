using Asp.Versioning;
using DonkeyWork.Agents.Agents.Contracts.Models;
using DonkeyWork.Agents.Agents.Contracts.Services;
using DonkeyWork.Agents.Identity.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DonkeyWork.Agents.Agents.Api.Controllers;

/// <summary>
/// Manage agent versions (draft/publish workflow).
/// </summary>
[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/agents/{agentId:guid}/versions")]
[Authorize]
[Produces("application/json")]
public class AgentVersionsController : ControllerBase
{
    private readonly IAgentVersionService _agentVersionService;
    private readonly IIdentityContext _identityContext;

    public AgentVersionsController(
        IAgentVersionService agentVersionService,
        IIdentityContext identityContext)
    {
        _agentVersionService = agentVersionService;
        _identityContext = identityContext;
    }

    /// <summary>
    /// Save draft version (creates new or updates existing draft).
    /// </summary>
    /// <param name="agentId">The agent ID.</param>
    /// <param name="request">The version data (ReactFlowData, NodeConfigurations, schemas).</param>
    /// <response code="200">Returns the saved draft version.</response>
    /// <response code="404">Agent not found.</response>
    /// <response code="400">Invalid request.</response>
    [HttpPost]
    [ProducesResponseType<GetAgentVersionResponseV1>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SaveDraft(Guid agentId, [FromBody] SaveAgentVersionRequestV1 request)
    {
        try
        {
            var version = await _agentVersionService.SaveDraftAsync(agentId, request, _identityContext.UserId);
            return Ok(version);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Publish the current draft version.
    /// </summary>
    /// <param name="agentId">The agent ID.</param>
    /// <response code="200">Returns the published version.</response>
    /// <response code="404">Agent or draft not found.</response>
    [HttpPost("publish")]
    [ProducesResponseType<GetAgentVersionResponseV1>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Publish(Guid agentId)
    {
        try
        {
            var version = await _agentVersionService.PublishAsync(agentId, _identityContext.UserId);
            return Ok(version);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get a specific version by ID.
    /// </summary>
    /// <param name="agentId">The agent ID.</param>
    /// <param name="versionId">The version ID.</param>
    /// <response code="200">Returns the version with full schema and configuration data.</response>
    /// <response code="404">Version not found.</response>
    [HttpGet("{versionId:guid}")]
    [ProducesResponseType<GetAgentVersionResponseV1>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetVersion(Guid agentId, Guid versionId)
    {
        var version = await _agentVersionService.GetVersionAsync(agentId, versionId, _identityContext.UserId);

        if (version == null)
            return NotFound();

        return Ok(version);
    }

    /// <summary>
    /// List all versions for an agent.
    /// </summary>
    /// <param name="agentId">The agent ID.</param>
    /// <response code="200">Returns the list of versions ordered by version number descending.</response>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<GetAgentVersionResponseV1>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListVersions(Guid agentId)
    {
        var versions = await _agentVersionService.GetVersionsAsync(agentId, _identityContext.UserId);
        return Ok(versions);
    }
}
