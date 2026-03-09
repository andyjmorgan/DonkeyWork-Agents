using Asp.Versioning;
using DonkeyWork.Agents.AgentDefinitions.Contracts.Models;
using DonkeyWork.Agents.AgentDefinitions.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DonkeyWork.Agents.AgentDefinitions.Api.Controllers;

/// <summary>
/// Manages agent definition CRUD operations.
/// </summary>
[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/agent-definitions")]
[Authorize]
[Produces("application/json")]
public class AgentDefinitionsController : ControllerBase
{
    private readonly IAgentDefinitionService _service;

    public AgentDefinitionsController(IAgentDefinitionService service)
    {
        _service = service;
    }

    /// <summary>
    /// Lists all agent definitions for the current user.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<AgentDefinitionSummaryV1>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var results = await _service.ListAsync(cancellationToken);
        return Ok(results);
    }

    /// <summary>
    /// Gets an agent definition by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<AgentDefinitionDetailsV1>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.GetByIdAsync(id, cancellationToken);
        return result == null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Creates a new agent definition.
    /// </summary>
    [HttpPost]
    [ProducesResponseType<AgentDefinitionDetailsV1>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateAgentDefinitionRequestV1 request, CancellationToken cancellationToken)
    {
        var result = await _service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Updates an existing agent definition.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<AgentDefinitionDetailsV1>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAgentDefinitionRequestV1 request, CancellationToken cancellationToken)
    {
        var result = await _service.UpdateAsync(id, request, cancellationToken);
        return result == null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Deletes an agent definition.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _service.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
