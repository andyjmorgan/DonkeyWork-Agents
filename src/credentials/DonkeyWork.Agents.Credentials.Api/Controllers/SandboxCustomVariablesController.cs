using Asp.Versioning;
using DonkeyWork.Agents.Credentials.Contracts.Models;
using DonkeyWork.Agents.Credentials.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DonkeyWork.Agents.Credentials.Api.Controllers;

/// <summary>
/// Manage sandbox custom environment variables.
/// </summary>
[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/sandbox-custom-variables")]
[Authorize]
[Produces("application/json")]
public class SandboxCustomVariablesController : ControllerBase
{
    private readonly ISandboxCustomVariableService _service;

    public SandboxCustomVariablesController(ISandboxCustomVariableService service)
    {
        _service = service;
    }

    /// <summary>
    /// List all sandbox custom variables for the current user.
    /// </summary>
    /// <response code="200">Returns the list of sandbox custom variables.</response>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<SandboxCustomVariableV1>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List()
    {
        var variables = await _service.ListAsync();
        return Ok(variables);
    }

    /// <summary>
    /// Get a specific sandbox custom variable.
    /// </summary>
    /// <param name="id">The variable ID.</param>
    /// <response code="200">Returns the sandbox custom variable.</response>
    /// <response code="404">Variable not found.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<SandboxCustomVariableV1>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id)
    {
        var variable = await _service.GetByIdAsync(id);

        if (variable is null)
            return NotFound();

        return Ok(variable);
    }

    /// <summary>
    /// Create a new sandbox custom variable.
    /// </summary>
    /// <param name="request">The variable details.</param>
    /// <response code="201">Returns the created variable.</response>
    /// <response code="400">Invalid request or duplicate key.</response>
    [HttpPost]
    [ProducesResponseType<SandboxCustomVariableV1>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateSandboxCustomVariableRequestV1 request)
    {
        try
        {
            var variable = await _service.CreateAsync(request);
            return CreatedAtAction(nameof(Get), new { id = variable.Id }, variable);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Update a sandbox custom variable.
    /// </summary>
    /// <param name="id">The variable ID.</param>
    /// <param name="request">The updated variable details.</param>
    /// <response code="200">Returns the updated variable.</response>
    /// <response code="404">Variable not found.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<SandboxCustomVariableV1>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSandboxCustomVariableRequestV1 request)
    {
        var existing = await _service.GetByIdAsync(id);
        if (existing is null)
            return NotFound();

        try
        {
            var variable = await _service.UpdateAsync(id, request);
            return Ok(variable);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Delete a sandbox custom variable.
    /// </summary>
    /// <param name="id">The variable ID.</param>
    /// <response code="204">Variable deleted.</response>
    /// <response code="404">Variable not found.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var existing = await _service.GetByIdAsync(id);
        if (existing is null)
            return NotFound();

        await _service.DeleteAsync(id);
        return NoContent();
    }
}
