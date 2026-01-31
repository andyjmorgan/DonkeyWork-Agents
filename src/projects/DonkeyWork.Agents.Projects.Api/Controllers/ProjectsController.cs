using Asp.Versioning;
using DonkeyWork.Agents.Projects.Contracts.Models;
using DonkeyWork.Agents.Projects.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DonkeyWork.Agents.Projects.Api.Controllers;

/// <summary>
/// Manage projects.
/// </summary>
[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/projects")]
[Authorize]
[Produces("application/json")]
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _projectService;

    public ProjectsController(IProjectService projectService)
    {
        _projectService = projectService;
    }

    /// <summary>
    /// Create a new project.
    /// </summary>
    /// <param name="request">The project details.</param>
    /// <response code="201">Returns the created project.</response>
    /// <response code="400">Invalid request.</response>
    [HttpPost]
    [ProducesResponseType<ProjectDetailsV1>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateProjectRequestV1 request)
    {
        var project = await _projectService.CreateAsync(request);
        return CreatedAtAction(nameof(Get), new { id = project.Id }, project);
    }

    /// <summary>
    /// Get a specific project by ID with full details.
    /// </summary>
    /// <param name="id">The project ID.</param>
    /// <response code="200">Returns the project.</response>
    /// <response code="404">Project not found.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<ProjectDetailsV1>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id)
    {
        var project = await _projectService.GetByIdAsync(id);

        if (project == null)
            return NotFound();

        return Ok(project);
    }

    /// <summary>
    /// List all projects for the current user.
    /// </summary>
    /// <response code="200">Returns the list of projects.</response>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ProjectSummaryV1>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List()
    {
        var projects = await _projectService.ListAsync();
        return Ok(projects);
    }

    /// <summary>
    /// Update a project.
    /// </summary>
    /// <param name="id">The project ID.</param>
    /// <param name="request">The updated project details.</param>
    /// <response code="200">Returns the updated project.</response>
    /// <response code="404">Project not found.</response>
    /// <response code="400">Invalid request.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<ProjectDetailsV1>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProjectRequestV1 request)
    {
        var project = await _projectService.UpdateAsync(id, request);

        if (project == null)
            return NotFound();

        return Ok(project);
    }

    /// <summary>
    /// Delete a project and all its related data.
    /// </summary>
    /// <param name="id">The project ID.</param>
    /// <response code="204">Project deleted.</response>
    /// <response code="404">Project not found.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _projectService.DeleteAsync(id);

        if (!deleted)
            return NotFound();

        return NoContent();
    }
}
