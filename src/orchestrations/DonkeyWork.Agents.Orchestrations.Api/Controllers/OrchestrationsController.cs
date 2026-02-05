using Asp.Versioning;
using DonkeyWork.Agents.Orchestrations.Contracts.Models;
using DonkeyWork.Agents.Orchestrations.Contracts.Services;
using DonkeyWork.Agents.Identity.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DonkeyWork.Agents.Orchestrations.Api.Controllers;

/// <summary>
/// Manage agents (workflow definitions).
/// </summary>
[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/orchestrations")]
[Authorize]
[Produces("application/json")]
public class OrchestrationsController : ControllerBase
{
    private readonly IOrchestrationService _agentService;
    private readonly IIdentityContext _identityContext;

    public OrchestrationsController(
        IOrchestrationService agentService,
        IIdentityContext identityContext)
    {
        _agentService = agentService;
        _identityContext = identityContext;
    }

    /// <summary>
    /// Create a new agent with default Start -> End template.
    /// </summary>
    /// <param name="request">The agent details.</param>
    /// <response code="201">Returns the created agent with initial draft version.</response>
    /// <response code="400">Invalid request.</response>
    [HttpPost]
    [ProducesResponseType<CreateOrchestrationResponseV1>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateOrchestrationRequestV1 request)
    {
        var agent = await _agentService.CreateAsync(request, _identityContext.UserId);
        return CreatedAtAction(nameof(Get), new { id = agent.Id }, agent);
    }

    /// <summary>
    /// Get a specific agent by ID.
    /// </summary>
    /// <param name="id">The agent ID.</param>
    /// <response code="200">Returns the agent.</response>
    /// <response code="404">Agent not found.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<GetOrchestrationResponseV1>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id)
    {
        var agent = await _agentService.GetByIdAsync(id, _identityContext.UserId);

        if (agent == null)
            return NotFound();

        return Ok(agent);
    }

    /// <summary>
    /// List all agents for the current user.
    /// </summary>
    /// <response code="200">Returns the list of agents.</response>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<GetOrchestrationResponseV1>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List()
    {
        var agents = await _agentService.GetByUserIdAsync(_identityContext.UserId);
        return Ok(agents);
    }

    /// <summary>
    /// List all agents with Chat interface enabled (for agent selector).
    /// </summary>
    /// <response code="200">Returns the list of chat-enabled agents.</response>
    [HttpGet("chat-enabled")]
    [ProducesResponseType<IReadOnlyList<ChatEnabledOrchestrationV1>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListChatEnabled()
    {
        var agents = await _agentService.ListChatEnabledAsync();
        return Ok(agents);
    }

    /// <summary>
    /// Update agent metadata (name, description).
    /// </summary>
    /// <param name="id">The agent ID.</param>
    /// <param name="request">The updated agent details.</param>
    /// <response code="200">Returns the updated agent.</response>
    /// <response code="404">Agent not found.</response>
    /// <response code="400">Invalid request.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<GetOrchestrationResponseV1>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateOrchestrationRequestV1 request)
    {
        var agent = await _agentService.UpdateAsync(id, request, _identityContext.UserId);

        if (agent == null)
            return NotFound();

        return Ok(agent);
    }

    /// <summary>
    /// Delete an agent and all its versions.
    /// </summary>
    /// <param name="id">The agent ID.</param>
    /// <response code="204">Agent deleted.</response>
    /// <response code="404">Agent not found.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _agentService.DeleteAsync(id, _identityContext.UserId);

        if (!deleted)
            return NotFound();

        return NoContent();
    }
}
