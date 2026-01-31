using Asp.Versioning;
using DonkeyWork.Agents.Identity.Contracts.Services;
using DonkeyWork.Agents.Projects.Contracts.Models;
using DonkeyWork.Agents.Projects.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DonkeyWork.Agents.Projects.Api.Controllers;

/// <summary>
/// Manage todos (standalone and within projects/milestones).
/// </summary>
[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/todos")]
[Authorize]
[Produces("application/json")]
public class TodosController : ControllerBase
{
    private readonly ITodoService _todoService;
    private readonly IIdentityContext _identityContext;

    public TodosController(
        ITodoService todoService,
        IIdentityContext identityContext)
    {
        _todoService = todoService;
        _identityContext = identityContext;
    }

    /// <summary>
    /// Create a new todo (standalone or within a project/milestone).
    /// </summary>
    /// <param name="request">The todo details.</param>
    /// <response code="201">Returns the created todo.</response>
    /// <response code="400">Invalid request.</response>
    [HttpPost]
    [ProducesResponseType<TodoV1>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateTodoRequestV1 request)
    {
        var todo = await _todoService.CreateAsync(request, _identityContext.UserId);
        return CreatedAtAction(nameof(Get), new { id = todo.Id }, todo);
    }

    /// <summary>
    /// Get a specific todo by ID.
    /// </summary>
    /// <param name="id">The todo ID.</param>
    /// <response code="200">Returns the todo.</response>
    /// <response code="404">Todo not found.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<TodoV1>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id)
    {
        var todo = await _todoService.GetByIdAsync(id, _identityContext.UserId);

        if (todo == null)
            return NotFound();

        return Ok(todo);
    }

    /// <summary>
    /// List all standalone todos for the current user (not associated with any project or milestone).
    /// </summary>
    /// <response code="200">Returns the list of standalone todos.</response>
    [HttpGet("standalone")]
    [ProducesResponseType<IReadOnlyList<TodoV1>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListStandalone()
    {
        var todos = await _todoService.GetStandaloneAsync(_identityContext.UserId);
        return Ok(todos);
    }

    /// <summary>
    /// List all todos for the current user.
    /// </summary>
    /// <response code="200">Returns the list of todos.</response>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<TodoV1>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List()
    {
        var todos = await _todoService.GetByUserIdAsync(_identityContext.UserId);
        return Ok(todos);
    }

    /// <summary>
    /// Update a todo.
    /// </summary>
    /// <param name="id">The todo ID.</param>
    /// <param name="request">The updated todo details.</param>
    /// <response code="200">Returns the updated todo.</response>
    /// <response code="404">Todo not found.</response>
    /// <response code="400">Invalid request.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<TodoV1>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTodoRequestV1 request)
    {
        var todo = await _todoService.UpdateAsync(id, request, _identityContext.UserId);

        if (todo == null)
            return NotFound();

        return Ok(todo);
    }

    /// <summary>
    /// Delete a todo.
    /// </summary>
    /// <param name="id">The todo ID.</param>
    /// <response code="204">Todo deleted.</response>
    /// <response code="404">Todo not found.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _todoService.DeleteAsync(id, _identityContext.UserId);

        if (!deleted)
            return NotFound();

        return NoContent();
    }
}
