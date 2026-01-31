using Asp.Versioning;
using DonkeyWork.Agents.Projects.Contracts.Models;
using DonkeyWork.Agents.Projects.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DonkeyWork.Agents.Projects.Api.Controllers;

/// <summary>
/// Manage milestones within projects.
/// </summary>
[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/projects/{projectId:guid}/milestones")]
[Authorize]
[Produces("application/json")]
public class MilestonesController : ControllerBase
{
    private readonly IMilestoneService _milestoneService;

    public MilestonesController(IMilestoneService milestoneService)
    {
        _milestoneService = milestoneService;
    }

    /// <summary>
    /// Create a new milestone within a project.
    /// </summary>
    /// <param name="projectId">The project ID.</param>
    /// <param name="request">The milestone details.</param>
    /// <response code="201">Returns the created milestone.</response>
    /// <response code="400">Invalid request.</response>
    /// <response code="404">Project not found.</response>
    [HttpPost]
    [ProducesResponseType<MilestoneDetailsV1>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(Guid projectId, [FromBody] CreateMilestoneRequestV1 request)
    {
        var milestone = await _milestoneService.CreateAsync(projectId, request);

        if (milestone == null)
            return NotFound("Project not found");

        return CreatedAtAction(nameof(Get), new { projectId, id = milestone.Id }, milestone);
    }

    /// <summary>
    /// Get a specific milestone by ID with full details.
    /// </summary>
    /// <param name="projectId">The project ID.</param>
    /// <param name="id">The milestone ID.</param>
    /// <response code="200">Returns the milestone.</response>
    /// <response code="404">Milestone not found.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<MilestoneDetailsV1>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid projectId, Guid id)
    {
        var milestone = await _milestoneService.GetByIdAsync(id);

        if (milestone == null || milestone.ProjectId != projectId)
            return NotFound();

        return Ok(milestone);
    }

    /// <summary>
    /// List all milestones for a project.
    /// </summary>
    /// <param name="projectId">The project ID.</param>
    /// <response code="200">Returns the list of milestones.</response>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<MilestoneSummaryV1>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List(Guid projectId)
    {
        var milestones = await _milestoneService.GetByProjectIdAsync(projectId);
        return Ok(milestones);
    }

    /// <summary>
    /// Update a milestone.
    /// </summary>
    /// <param name="projectId">The project ID.</param>
    /// <param name="id">The milestone ID.</param>
    /// <param name="request">The updated milestone details.</param>
    /// <response code="200">Returns the updated milestone.</response>
    /// <response code="404">Milestone not found.</response>
    /// <response code="400">Invalid request.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<MilestoneDetailsV1>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(Guid projectId, Guid id, [FromBody] UpdateMilestoneRequestV1 request)
    {
        var milestone = await _milestoneService.UpdateAsync(id, request);

        if (milestone == null)
            return NotFound();

        return Ok(milestone);
    }

    /// <summary>
    /// Delete a milestone and all its related data.
    /// </summary>
    /// <param name="projectId">The project ID.</param>
    /// <param name="id">The milestone ID.</param>
    /// <response code="204">Milestone deleted.</response>
    /// <response code="404">Milestone not found.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid projectId, Guid id)
    {
        var deleted = await _milestoneService.DeleteAsync(id);

        if (!deleted)
            return NotFound();

        return NoContent();
    }
}
