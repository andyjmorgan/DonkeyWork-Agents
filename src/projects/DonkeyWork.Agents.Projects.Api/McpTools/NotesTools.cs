using System.ComponentModel;
using DonkeyWork.Agents.Mcp.Contracts;
using DonkeyWork.Agents.Projects.Contracts.Models;
using DonkeyWork.Agents.Projects.Contracts.Services;
using ModelContextProtocol.Server;

namespace DonkeyWork.Agents.Projects.Api.McpTools;

/// <summary>
/// MCP tools for managing notes.
/// </summary>
[McpServerToolType]
[McpToolProvider(Provider = McpToolProvider.DonkeyWork)]
public class NotesTools
{
    private readonly INoteService _noteService;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotesTools"/> class.
    /// </summary>
    public NotesTools(INoteService noteService)
    {
        _noteService = noteService;
    }

    /// <summary>
    /// Lists all notes for the current user.
    /// </summary>
    [McpServerTool(Name = "notes_list")]
    [McpTool(
        Name = "notes_list",
        Title = "List Notes",
        Description = "List all notes for the current user",
        Icon = "list",
        ReadOnlyHint = true)]
    public async Task<IReadOnlyList<NoteV1>> ListNotes(CancellationToken ct)
    {
        return await _noteService.ListAsync(ct);
    }

    /// <summary>
    /// Gets a note by ID.
    /// </summary>
    [McpServerTool(Name = "notes_get")]
    [McpTool(
        Name = "notes_get",
        Title = "Get Note",
        Description = "Get a note by ID",
        Icon = "file",
        ReadOnlyHint = true)]
    public async Task<NoteV1?> GetNote(
        [Description("The unique identifier of the note")] Guid id,
        CancellationToken ct)
    {
        return await _noteService.GetByIdAsync(id, ct);
    }

    /// <summary>
    /// Creates a new note.
    /// </summary>
    [McpServerTool(Name = "notes_create")]
    [McpTool(
        Name = "notes_create",
        Title = "Create Note",
        Description = "Create a new note",
        Icon = "plus")]
    public async Task<NoteV1> CreateNote(
        [Description("The title of the note")] string title,
        [Description("Optional content of the note")] string? content,
        [Description("Optional sort order for display")] int? sortOrder,
        [Description("Optional project ID to associate the note with")] Guid? projectId,
        [Description("Optional milestone ID to associate the note with")] Guid? milestoneId,
        CancellationToken ct)
    {
        var request = new CreateNoteRequestV1
        {
            Title = title,
            Content = content,
            SortOrder = sortOrder ?? 0,
            ProjectId = projectId,
            MilestoneId = milestoneId
        };

        return await _noteService.CreateAsync(request, ct);
    }

    /// <summary>
    /// Updates an existing note.
    /// </summary>
    [McpServerTool(Name = "notes_update")]
    [McpTool(
        Name = "notes_update",
        Title = "Update Note",
        Description = "Update an existing note",
        Icon = "edit")]
    public async Task<NoteV1?> UpdateNote(
        [Description("The unique identifier of the note to update")] Guid id,
        [Description("The new title of the note")] string title,
        [Description("Optional new content of the note")] string? content,
        [Description("Optional new sort order for display")] int? sortOrder,
        [Description("Optional project ID to associate the note with")] Guid? projectId,
        [Description("Optional milestone ID to associate the note with")] Guid? milestoneId,
        CancellationToken ct)
    {
        var request = new UpdateNoteRequestV1
        {
            Title = title,
            Content = content,
            SortOrder = sortOrder ?? 0,
            ProjectId = projectId,
            MilestoneId = milestoneId
        };

        return await _noteService.UpdateAsync(id, request, ct);
    }

    /// <summary>
    /// Permanently deletes a note.
    /// </summary>
    [McpServerTool(Name = "notes_delete")]
    [McpTool(
        Name = "notes_delete",
        Title = "Delete Note",
        Description = "Permanently delete a note",
        Icon = "trash",
        DestructiveHint = true,
        IdempotentHint = true)]
    public async Task<bool> DeleteNote(
        [Description("The unique identifier of the note to delete")] Guid id,
        CancellationToken ct)
    {
        return await _noteService.DeleteAsync(id, ct);
    }

    /// <summary>
    /// Lists all notes for a specific project.
    /// </summary>
    [McpServerTool(Name = "notes_list_by_project")]
    [McpTool(
        Name = "notes_list_by_project",
        Title = "List Notes by Project",
        Description = "List all notes for a specific project",
        Icon = "list",
        ReadOnlyHint = true)]
    public async Task<IReadOnlyList<NoteV1>> ListNotesByProject(
        [Description("The unique identifier of the project")] Guid projectId,
        CancellationToken ct)
    {
        return await _noteService.GetByProjectIdAsync(projectId, ct);
    }

    /// <summary>
    /// Lists all notes for a specific milestone.
    /// </summary>
    [McpServerTool(Name = "notes_list_by_milestone")]
    [McpTool(
        Name = "notes_list_by_milestone",
        Title = "List Notes by Milestone",
        Description = "List all notes for a specific milestone",
        Icon = "list",
        ReadOnlyHint = true)]
    public async Task<IReadOnlyList<NoteV1>> ListNotesByMilestone(
        [Description("The unique identifier of the milestone")] Guid milestoneId,
        CancellationToken ct)
    {
        return await _noteService.GetByMilestoneIdAsync(milestoneId, ct);
    }
}
