using System.ComponentModel;
using DonkeyWork.Agents.Mcp.Contracts;
using DonkeyWork.Agents.Projects.Contracts.Models;
using DonkeyWork.Agents.Projects.Contracts.Services;
using ModelContextProtocol.Server;

namespace DonkeyWork.Agents.Projects.Api.McpTools;

/// <summary>
/// MCP tools for managing milestones.
/// </summary>
[McpServerToolType]
[McpToolProvider(Provider = McpToolProvider.DonkeyWork)]
public class MilestonesTools
{
    private readonly IMilestoneService _milestoneService;

    /// <summary>
    /// Initializes a new instance of the <see cref="MilestonesTools"/> class.
    /// </summary>
    public MilestonesTools(IMilestoneService milestoneService)
    {
        _milestoneService = milestoneService;
    }

    /// <summary>
    /// Lists all milestones for a specific project.
    /// Returns summary models without content/successCriteria - use milestones_get for full details.
    /// </summary>
    [McpServerTool(Name = "milestones_list")]
    [McpTool(
        Name = "milestones_list",
        Title = "List Milestones",
        Description = "List all milestones for a specific project. Milestones are phases or deliverables within a project. Each milestone can have its own tasks and notes. Returns summary models - use milestones_get for full milestone content.",
        Icon = "list",
        ReadOnlyHint = true)]
    public async Task<IReadOnlyList<MilestoneSummaryV1>> ListMilestones(
        [Description("The unique identifier of the project to list milestones for")] Guid projectId,
        CancellationToken ct)
    {
        return await _milestoneService.GetByProjectIdAsync(projectId, ct);
    }

    /// <summary>
    /// Gets a milestone by ID with full details.
    /// </summary>
    [McpServerTool(Name = "milestones_get")]
    [McpTool(
        Name = "milestones_get",
        Title = "Get Milestone",
        Description = "Get a milestone by ID with full details including all tasks and notes associated with this milestone.",
        Icon = "file",
        ReadOnlyHint = true)]
    public async Task<MilestoneDetailsV1?> GetMilestone(
        [Description("The unique identifier of the milestone")] Guid id,
        [Description("Optional character offset to start reading content from (for chunked reading of large content fields)")] int? contentOffset = null,
        [Description("Optional number of characters to read from the offset (for chunked reading of large content fields)")] int? contentLength = null,
        CancellationToken ct = default)
    {
        return await _milestoneService.GetByIdAsync(id, contentOffset, contentLength, ct);
    }

    /// <summary>
    /// Creates a new milestone within a project.
    /// </summary>
    [McpServerTool(Name = "milestones_create")]
    [McpTool(
        Name = "milestones_create",
        Title = "Create Milestone",
        Description = "Create a new milestone within a project. Milestones represent major phases or deliverables. After creating a milestone, you can add tasks and notes to it using tasks_create and notes_create with the milestoneId parameter.",
        Icon = "plus")]
    public async Task<MilestoneDetailsV1?> CreateMilestone(
        [Description("The project ID this milestone belongs to (required - milestones must belong to a project)")] Guid projectId,
        [Description("The name of the milestone")] string name,
        [Description("Optional content/description of the milestone (supports markdown and mermaid diagrams)")] string? content,
        [Description("Optional success criteria for the milestone")] string? successCriteria,
        [Description("Status: NotStarted (default), InProgress, OnHold, Completed, or Cancelled")] MilestoneStatus? status,
        [Description("Optional due date for the milestone")] DateTimeOffset? dueDate,
        [Description("Optional sort order for display")] int? sortOrder,
        CancellationToken ct)
    {
        var request = new CreateMilestoneRequestV1
        {
            Name = name,
            Content = content,
            SuccessCriteria = successCriteria,
            Status = status ?? MilestoneStatus.NotStarted,
            DueDate = dueDate,
            SortOrder = sortOrder ?? 0
        };

        return await _milestoneService.CreateAsync(projectId, request, ct);
    }

    /// <summary>
    /// Updates an existing milestone. Only provided fields are updated; omitted fields retain their current values.
    /// </summary>
    [McpServerTool(Name = "milestones_update")]
    [McpTool(
        Name = "milestones_update",
        Title = "Update Milestone",
        Description = "Update an existing milestone's details. Only provided fields are updated - omit fields to keep their current values. Does not affect tasks or notes within the milestone.",
        Icon = "edit")]
    public async Task<UpdateAcknowledgmentV1?> UpdateMilestone(
        [Description("The unique identifier of the milestone to update")] Guid id,
        [Description("New name for the milestone (omit to keep current)")] string? name = null,
        [Description("New content/description (supports markdown and mermaid diagrams, omit to keep current). IMPORTANT: Only provide this if you need to change the content - the entire content is sent over the wire, so avoid unnecessary updates.")] string? content = null,
        [Description("New success criteria (omit to keep current)")] string? successCriteria = null,
        [Description("Status: NotStarted, InProgress, OnHold, Completed, or Cancelled (omit to keep current)")] MilestoneStatus? status = null,
        [Description("Completion notes (set when marking as Completed or Cancelled, omit to keep current)")] string? completionNotes = null,
        [Description("New due date (omit to keep current)")] DateTimeOffset? dueDate = null,
        [Description("New sort order for display (omit to keep current)")] int? sortOrder = null,
        CancellationToken ct = default)
    {
        // Fetch current milestone to merge with provided values
        var current = await _milestoneService.GetByIdAsync(id, cancellationToken: ct);
        if (current == null)
        {
            return null;
        }

        var request = new UpdateMilestoneRequestV1
        {
            Name = name ?? current.Name,
            Content = content ?? current.Content,
            SuccessCriteria = successCriteria ?? current.SuccessCriteria,
            Status = status ?? current.Status,
            CompletionNotes = completionNotes ?? current.CompletionNotes,
            DueDate = dueDate ?? current.DueDate,
            SortOrder = sortOrder ?? current.SortOrder
        };

        var updated = await _milestoneService.UpdateAsync(id, request, ct);
        if (updated == null)
        {
            return null;
        }

        return new UpdateAcknowledgmentV1 { Id = id, Status = "updated" };
    }

    /// <summary>
    /// Permanently deletes a milestone and all its related data.
    /// </summary>
    [McpServerTool(Name = "milestones_delete")]
    [McpTool(
        Name = "milestones_delete",
        Title = "Delete Milestone",
        Description = "Permanently delete a milestone and all its related data (tasks, notes)",
        Icon = "trash",
        DestructiveHint = true,
        IdempotentHint = true)]
    public async Task<bool> DeleteMilestone(
        [Description("The unique identifier of the milestone to delete")] Guid id,
        CancellationToken ct)
    {
        return await _milestoneService.DeleteAsync(id, ct);
    }
}
