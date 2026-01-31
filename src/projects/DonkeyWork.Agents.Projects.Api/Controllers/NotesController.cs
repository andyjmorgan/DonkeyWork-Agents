using Asp.Versioning;
using DonkeyWork.Agents.Projects.Contracts.Models;
using DonkeyWork.Agents.Projects.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DonkeyWork.Agents.Projects.Api.Controllers;

/// <summary>
/// Manage notes (standalone and within projects/milestones).
/// </summary>
[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/notes")]
[Authorize]
[Produces("application/json")]
public class NotesController : ControllerBase
{
    private readonly INoteService _noteService;

    public NotesController(INoteService noteService)
    {
        _noteService = noteService;
    }

    /// <summary>
    /// Create a new note (standalone or within a project/milestone).
    /// </summary>
    /// <param name="request">The note details.</param>
    /// <response code="201">Returns the created note.</response>
    /// <response code="400">Invalid request.</response>
    [HttpPost]
    [ProducesResponseType<NoteV1>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateNoteRequestV1 request)
    {
        var note = await _noteService.CreateAsync(request);
        return CreatedAtAction(nameof(Get), new { id = note.Id }, note);
    }

    /// <summary>
    /// Get a specific note by ID.
    /// </summary>
    /// <param name="id">The note ID.</param>
    /// <response code="200">Returns the note.</response>
    /// <response code="404">Note not found.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<NoteV1>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id)
    {
        var note = await _noteService.GetByIdAsync(id);

        if (note == null)
            return NotFound();

        return Ok(note);
    }

    /// <summary>
    /// List all standalone notes for the current user (not associated with any project or milestone).
    /// </summary>
    /// <response code="200">Returns the list of standalone notes.</response>
    [HttpGet("standalone")]
    [ProducesResponseType<IReadOnlyList<NoteV1>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListStandalone()
    {
        var notes = await _noteService.GetStandaloneAsync();
        return Ok(notes);
    }

    /// <summary>
    /// List all notes for the current user.
    /// </summary>
    /// <response code="200">Returns the list of notes.</response>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<NoteV1>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List()
    {
        var notes = await _noteService.ListAsync();
        return Ok(notes);
    }

    /// <summary>
    /// Update a note.
    /// </summary>
    /// <param name="id">The note ID.</param>
    /// <param name="request">The updated note details.</param>
    /// <response code="200">Returns the updated note.</response>
    /// <response code="404">Note not found.</response>
    /// <response code="400">Invalid request.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<NoteV1>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateNoteRequestV1 request)
    {
        var note = await _noteService.UpdateAsync(id, request);

        if (note == null)
            return NotFound();

        return Ok(note);
    }

    /// <summary>
    /// Delete a note.
    /// </summary>
    /// <param name="id">The note ID.</param>
    /// <response code="204">Note deleted.</response>
    /// <response code="404">Note not found.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _noteService.DeleteAsync(id);

        if (!deleted)
            return NotFound();

        return NoContent();
    }
}
