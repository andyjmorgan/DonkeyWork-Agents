using Asp.Versioning;
using DonkeyWork.Agents.Projects.Contracts.Models;
using DonkeyWork.Agents.Projects.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DonkeyWork.Agents.Projects.Api.Controllers;

/// <summary>
/// Manage research items.
/// </summary>
[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/research")]
[Authorize]
[Produces("application/json")]
public class ResearchController : ControllerBase
{
    private readonly IResearchService _researchService;

    public ResearchController(IResearchService researchService)
    {
        _researchService = researchService;
    }

    /// <summary>
    /// Create a new research item.
    /// </summary>
    /// <param name="request">The research details.</param>
    /// <response code="201">Returns the created research item.</response>
    /// <response code="400">Invalid request.</response>
    [HttpPost]
    [ProducesResponseType<ResearchDetailsV1>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateResearchRequestV1 request)
    {
        var research = await _researchService.CreateAsync(request);
        return CreatedAtAction(nameof(Get), new { id = research.Id }, research);
    }

    /// <summary>
    /// Get a specific research item by ID.
    /// </summary>
    /// <param name="id">The research item ID.</param>
    /// <response code="200">Returns the research item.</response>
    /// <response code="404">Research item not found.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<ResearchDetailsV1>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id)
    {
        var research = await _researchService.GetByIdAsync(id);

        if (research == null)
            return NotFound();

        return Ok(research);
    }

    /// <summary>
    /// List all research items for the current user.
    /// </summary>
    /// <response code="200">Returns the list of research items.</response>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ResearchSummaryV1>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List()
    {
        var items = await _researchService.ListAsync();
        return Ok(items);
    }

    /// <summary>
    /// Update a research item.
    /// </summary>
    /// <param name="id">The research item ID.</param>
    /// <param name="request">The updated research details.</param>
    /// <response code="200">Returns the updated research item.</response>
    /// <response code="404">Research item not found.</response>
    /// <response code="400">Invalid request.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<ResearchDetailsV1>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateResearchRequestV1 request)
    {
        try
        {
            var research = await _researchService.UpdateAsync(id, request);

            if (research == null)
                return NotFound();

            return Ok(research);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Delete a research item.
    /// </summary>
    /// <param name="id">The research item ID.</param>
    /// <response code="204">Research item deleted.</response>
    /// <response code="404">Research item not found.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _researchService.DeleteAsync(id);

        if (!deleted)
            return NotFound();

        return NoContent();
    }
}
