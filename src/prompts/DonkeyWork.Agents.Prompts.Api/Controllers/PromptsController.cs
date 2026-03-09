using Asp.Versioning;
using DonkeyWork.Agents.Prompts.Contracts.Models;
using DonkeyWork.Agents.Prompts.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DonkeyWork.Agents.Prompts.Api.Controllers;

/// <summary>
/// Manages prompt CRUD operations.
/// </summary>
[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/prompts")]
[Authorize]
[Produces("application/json")]
public class PromptsController : ControllerBase
{
    private readonly IPromptService _service;

    public PromptsController(IPromptService service)
    {
        _service = service;
    }

    /// <summary>
    /// Lists all prompts for the current user.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<PromptSummaryV1>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var results = await _service.ListAsync(cancellationToken);
        return Ok(results);
    }

    /// <summary>
    /// Gets a prompt by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<PromptDetailsV1>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.GetByIdAsync(id, cancellationToken);
        return result == null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Creates a new prompt.
    /// </summary>
    [HttpPost]
    [ProducesResponseType<PromptDetailsV1>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreatePromptRequestV1 request, CancellationToken cancellationToken)
    {
        var result = await _service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Updates an existing prompt.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<PromptDetailsV1>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePromptRequestV1 request, CancellationToken cancellationToken)
    {
        var result = await _service.UpdateAsync(id, request, cancellationToken);
        return result == null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Deletes a prompt.
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
