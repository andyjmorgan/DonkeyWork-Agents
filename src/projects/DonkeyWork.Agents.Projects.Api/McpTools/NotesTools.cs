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
    /// Returns summary models without content - use notes_get for full details.
    /// </summary>
    [McpServerTool(Name = "notes_list")]
    [McpTool(
        Name = "notes_list",
        Title = "List Notes",
        Description = "List all notes for the current user. Notes can be standalone, or optionally associated with a project or milestone. Returns summary models - use notes_get for full note content.",
        Icon = "list",
        ReadOnlyHint = true)]
    public async Task<IReadOnlyList<NoteSummaryV1>> ListNotes(CancellationToken ct)
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
        [Description("Optional character offset to start reading content from (for chunked reading of large content fields)")] int? contentOffset = null,
        [Description("Optional number of characters to read from the offset (for chunked reading of large content fields)")] int? contentLength = null,
        CancellationToken ct = default)
    {
        return await _noteService.GetByIdAsync(id, contentOffset, contentLength, ct);
    }

    /// <summary>
    /// Creates a new note.
    /// </summary>
    [McpServerTool(Name = "notes_create")]
    [McpTool(
        Name = "notes_create",
        Title = "Create Note",
        Description = "Create a new note. Notes can be standalone, or associated with a project, milestone, or research item. Provide projectId for project-level notes, milestoneId for milestone-level notes, or researchId for research-level notes.",
        Icon = "plus")]
    public async Task<NoteV1> CreateNote(
        [Description("The title of the note")] string title,
        [Description("Optional content of the note (supports markdown and mermaid diagrams)")] string? content,
        [Description("Optional sort order for display")] int? sortOrder,
        [Description("Optional project ID - set to associate this note with a project (note becomes project-level)")] Guid? projectId,
        [Description("Optional milestone ID - set to associate this note with a milestone (note becomes milestone-level)")] Guid? milestoneId,
        [Description("Optional research ID - set to associate this note with a research item (note becomes research-level)")] Guid? researchId = null,
        CancellationToken ct = default)
    {
        var request = new CreateNoteRequestV1
        {
            Title = title,
            Content = content,
            SortOrder = sortOrder ?? 0,
            ProjectId = projectId,
            MilestoneId = milestoneId,
            ResearchId = researchId
        };

        return await _noteService.CreateAsync(request, ct);
    }

    /// <summary>
    /// Updates an existing note. Only provided fields are updated; omitted fields retain their current values.
    /// </summary>
    [McpServerTool(Name = "notes_update")]
    [McpTool(
        Name = "notes_update",
        Title = "Update Note",
        Description = "Update an existing note. IMPORTANT: Only `id` is required - all other parameters are optional. Do NOT pass fields you don't intend to change; omitted fields keep their current values automatically. For example, to change only the title, pass just `id` and `title`. You can move notes between standalone/project/milestone/research associations by setting projectId, milestoneId, or researchId.",
        Icon = "edit")]
    public async Task<UpdateAcknowledgmentV1?> UpdateNote(
        [Description("The unique identifier of the note to update")] Guid id,
        [Description("New title for the note (omit to keep current)")] string? title = null,
        [Description("New content (supports markdown, omit to keep current). IMPORTANT: Only provide this if you need to change the content - the entire content is sent over the wire, so avoid unnecessary updates.")] string? content = null,
        [Description("New sort order for display (omit to keep current)")] int? sortOrder = null,
        [Description("Project ID to associate with (omit to keep current)")] Guid? projectId = null,
        [Description("Milestone ID to associate with (omit to keep current)")] Guid? milestoneId = null,
        [Description("Research ID to associate with (omit to keep current)")] Guid? researchId = null,
        CancellationToken ct = default)
    {
        // Fetch current note to merge with provided values
        var current = await _noteService.GetByIdAsync(id, cancellationToken: ct);
        if (current == null)
        {
            return null;
        }

        var request = new UpdateNoteRequestV1
        {
            Title = title ?? current.Title,
            Content = content ?? current.Content,
            SortOrder = sortOrder ?? current.SortOrder,
            ProjectId = projectId ?? current.ProjectId,
            MilestoneId = milestoneId ?? current.MilestoneId,
            ResearchId = researchId ?? current.ResearchId
        };

        var updated = await _noteService.UpdateAsync(id, request, ct);
        if (updated == null)
        {
            return null;
        }

        return new UpdateAcknowledgmentV1 { Id = id, Status = "updated" };
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
    /// Returns summary models without content - use notes_get for full details.
    /// </summary>
    [McpServerTool(Name = "notes_list_by_project")]
    [McpTool(
        Name = "notes_list_by_project",
        Title = "List Notes by Project",
        Description = "List all notes directly associated with a specific project (project-level notes only). Does not include notes associated with milestones within the project - use notes_list_by_milestone for those. Returns summary models - use notes_get for full content.",
        Icon = "list",
        ReadOnlyHint = true)]
    public async Task<IReadOnlyList<NoteSummaryV1>> ListNotesByProject(
        [Description("The unique identifier of the project")] Guid projectId,
        CancellationToken ct)
    {
        return await _noteService.GetByProjectIdAsync(projectId, ct);
    }

    /// <summary>
    /// Lists all notes for a specific milestone.
    /// Returns summary models without content - use notes_get for full details.
    /// </summary>
    [McpServerTool(Name = "notes_list_by_milestone")]
    [McpTool(
        Name = "notes_list_by_milestone",
        Title = "List Notes by Milestone",
        Description = "List all notes associated with a specific milestone. Milestones belong to projects and represent major deliverables or phases. Returns summary models - use notes_get for full content.",
        Icon = "list",
        ReadOnlyHint = true)]
    public async Task<IReadOnlyList<NoteSummaryV1>> ListNotesByMilestone(
        [Description("The unique identifier of the milestone")] Guid milestoneId,
        CancellationToken ct)
    {
        return await _noteService.GetByMilestoneIdAsync(milestoneId, ct);
    }

    /// <summary>
    /// Lists all notes for a specific research item.
    /// Returns summary models without content - use notes_get for full details.
    /// </summary>
    [McpServerTool(Name = "notes_list_by_research")]
    [McpTool(
        Name = "notes_list_by_research",
        Title = "List Notes by Research",
        Description = "List all notes associated with a specific research item. Research items track investigation topics, and notes contain the research material and findings. Returns summary models - use notes_get for full content.",
        Icon = "list",
        ReadOnlyHint = true)]
    public async Task<IReadOnlyList<NoteSummaryV1>> ListNotesByResearch(
        [Description("The unique identifier of the research item")] Guid researchId,
        CancellationToken ct)
    {
        return await _noteService.GetByResearchIdAsync(researchId, ct);
    }
}
