using Asp.Versioning;
using DonkeyWork.Agents.Common.Contracts.Models.Pagination;
using DonkeyWork.Agents.Projects.Contracts.Models;
using DonkeyWork.Agents.Projects.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DonkeyWork.Agents.Projects.Api.Controllers;

/// <summary>
/// Manage tasks (standalone and within projects/milestones).
/// </summary>
[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/tasks")]
[Authorize]
[Produces("application/json")]
public class TasksController : ControllerBase
{
    private readonly ITaskItemService _taskItemService;

    public TasksController(ITaskItemService taskItemService)
    {
        _taskItemService = taskItemService;
    }

    /// <summary>
    /// Create a new task (standalone or within a project/milestone).
    /// </summary>
    /// <param name="request">The task details.</param>
    /// <response code="201">Returns the created task.</response>
    /// <response code="400">Invalid request.</response>
    [HttpPost]
    [ProducesResponseType<TaskItemV1>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateTaskItemRequestV1 request)
    {
        var task = await _taskItemService.CreateAsync(request);
        return CreatedAtAction(nameof(Get), new { id = task.Id }, task);
    }

    /// <summary>
    /// Get a specific task by ID.
    /// </summary>
    /// <param name="id">The task ID.</param>
    /// <response code="200">Returns the task.</response>
    /// <response code="404">Task not found.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<TaskItemV1>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id)
    {
        var task = await _taskItemService.GetByIdAsync(id);

        if (task == null)
            return NotFound();

        return Ok(task);
    }

    /// <summary>
    /// List all standalone tasks for the current user (not associated with any project or milestone).
    /// </summary>
    /// <response code="200">Returns the list of standalone tasks.</response>
    [HttpGet("standalone")]
    [ProducesResponseType<IReadOnlyList<TaskItemV1>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListStandalone()
    {
        var tasks = await _taskItemService.GetStandaloneAsync();
        return Ok(tasks);
    }

    /// <summary>
    /// List all tasks for the current user with optional filtering and pagination.
    /// </summary>
    /// <param name="pagination">Pagination parameters.</param>
    /// <param name="filter">Optional filter parameters.</param>
    /// <response code="200">Returns the paginated list of tasks.</response>
    [HttpGet]
    [ProducesResponseType<PaginatedResponse<TaskItemSummaryV1>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] PaginationRequest pagination, [FromQuery] TaskItemFilterRequestV1 filter)
    {
        var tasks = await _taskItemService.ListAsync(pagination, filter);
        return Ok(tasks);
    }

    /// <summary>
    /// Update a task.
    /// </summary>
    /// <param name="id">The task ID.</param>
    /// <param name="request">The updated task details.</param>
    /// <response code="200">Returns the updated task.</response>
    /// <response code="404">Task not found.</response>
    /// <response code="400">Invalid request.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<TaskItemV1>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTaskItemRequestV1 request)
    {
        var task = await _taskItemService.UpdateAsync(id, request);

        if (task == null)
            return NotFound();

        return Ok(task);
    }

    /// <summary>
    /// Delete a task.
    /// </summary>
    /// <param name="id">The task ID.</param>
    /// <response code="204">Task deleted.</response>
    /// <response code="404">Task not found.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _taskItemService.DeleteAsync(id);

        if (!deleted)
            return NotFound();

        return NoContent();
    }
}
