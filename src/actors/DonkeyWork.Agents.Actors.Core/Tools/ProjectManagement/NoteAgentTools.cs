using System.ComponentModel;
using DonkeyWork.Agents.Projects.Contracts.Models;
using DonkeyWork.Agents.Projects.Contracts.Services;

namespace DonkeyWork.Agents.Actors.Core.Tools.ProjectManagement;

public sealed class NoteAgentTools
{
    private readonly INoteService _noteService;

    public NoteAgentTools(INoteService noteService)
    {
        _noteService = noteService;
    }

    [AgentTool("notes_list", DisplayName = "List Notes")]
    [Description("List all notes for the current user.")]
    public async Task<ToolResult> ListNotes(CancellationToken ct = default)
    {
        var notes = await _noteService.ListAsync(ct);
        return ToolResult.Json(notes);
    }

    [AgentTool("notes_list_by_project", DisplayName = "List Notes by Project")]
    [Description("List all notes for a project.")]
    public async Task<ToolResult> ListNotesByProject(
        [Description("The project ID")] Guid projectId,
        CancellationToken ct = default)
    {
        var notes = await _noteService.GetByProjectIdAsync(projectId, ct);
        return ToolResult.Json(notes);
    }

    [AgentTool("notes_list_by_milestone", DisplayName = "List Notes by Milestone")]
    [Description("List all notes for a milestone.")]
    public async Task<ToolResult> ListNotesByMilestone(
        [Description("The milestone ID")] Guid milestoneId,
        CancellationToken ct = default)
    {
        var notes = await _noteService.GetByMilestoneIdAsync(milestoneId, ct);
        return ToolResult.Json(notes);
    }

    [AgentTool("notes_list_by_research", DisplayName = "List Notes by Research")]
    [Description("List all notes for a research item.")]
    public async Task<ToolResult> ListNotesByResearch(
        [Description("The research ID")] Guid researchId,
        CancellationToken ct = default)
    {
        var notes = await _noteService.GetByResearchIdAsync(researchId, ct);
        return ToolResult.Json(notes);
    }

    [AgentTool("notes_get", DisplayName = "Get Note")]
    [Description("Get a note by ID with full content.")]
    public async Task<ToolResult> GetNote(
        [Description("The note ID")] Guid noteId,
        [Description("Content offset for chunked reading")] int? contentOffset = null,
        [Description("Content length for chunked reading")] int? contentLength = null,
        CancellationToken ct = default)
    {
        var note = await _noteService.GetByIdAsync(noteId, contentOffset, contentLength, ct);
        return note is not null ? ToolResult.Json(note) : ToolResult.NotFound("Note", noteId);
    }

    [AgentTool("notes_create", DisplayName = "Create Note")]
    [Description("Create a new note, optionally associated with a project, milestone, or research item.")]
    public async Task<ToolResult> CreateNote(
        [Description("The note title")] string title,
        [Description("The note content")] string? content = null,
        [Description("Sort order")] int sortOrder = 0,
        [Description("Associated project ID")] Guid? projectId = null,
        [Description("Associated milestone ID")] Guid? milestoneId = null,
        [Description("Associated research ID")] Guid? researchId = null,
        CancellationToken ct = default)
    {
        var request = new CreateNoteRequestV1
        {
            Title = title,
            Content = content,
            SortOrder = sortOrder,
            ProjectId = projectId,
            MilestoneId = milestoneId,
            ResearchId = researchId,
        };
        var note = await _noteService.CreateAsync(request, ct);
        return ToolResult.Json(note);
    }

    [AgentTool("notes_update", DisplayName = "Update Note")]
    [Description("Update an existing note.")]
    public async Task<ToolResult> UpdateNote(
        [Description("The note ID")] Guid noteId,
        [Description("The note title")] string title,
        [Description("The note content")] string? content = null,
        [Description("Sort order")] int sortOrder = 0,
        [Description("Associated project ID")] Guid? projectId = null,
        [Description("Associated milestone ID")] Guid? milestoneId = null,
        [Description("Associated research ID")] Guid? researchId = null,
        CancellationToken ct = default)
    {
        var request = new UpdateNoteRequestV1
        {
            Title = title,
            Content = content,
            SortOrder = sortOrder,
            ProjectId = projectId,
            MilestoneId = milestoneId,
            ResearchId = researchId,
        };
        var note = await _noteService.UpdateAsync(noteId, request, ct);
        return note is not null ? ToolResult.Json(note) : ToolResult.NotFound("Note", noteId);
    }

    [AgentTool("notes_delete", DisplayName = "Delete Note")]
    [Description("Delete a note.")]
    public async Task<ToolResult> DeleteNote(
        [Description("The note ID")] Guid noteId,
        CancellationToken ct = default)
    {
        var deleted = await _noteService.DeleteAsync(noteId, ct);
        return deleted
            ? ToolResult.Success($"Note '{noteId}' deleted successfully.")
            : ToolResult.NotFound("Note", noteId);
    }
}
