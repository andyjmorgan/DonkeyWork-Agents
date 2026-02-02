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
    /// </summary>
    [McpServerTool(Name = "milestones_list")]
    [McpTool(
        Name = "milestones_list",
        Title = "List Milestones",
        Description = "List all milestones for a specific project",
        Icon = "list",
        ReadOnlyHint = true)]
    public async Task<IReadOnlyList<MilestoneSummaryV1>> ListMilestones(
        [Description("The unique identifier of the project")] Guid projectId,
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
        Description = "Get a milestone by ID with full details including todos and notes",
        Icon = "file",
        ReadOnlyHint = true)]
    public async Task<MilestoneDetailsV1?> GetMilestone(
        [Description("The unique identifier of the milestone")] Guid id,
        CancellationToken ct)
    {
        return await _milestoneService.GetByIdAsync(id, ct);
    }

    /// <summary>
    /// Creates a new milestone within a project.
    /// </summary>
    [McpServerTool(Name = "milestones_create")]
    [McpTool(
        Name = "milestones_create",
        Title = "Create Milestone",
        Description = "Create a new milestone within a project",
        Icon = "plus")]
    public async Task<MilestoneDetailsV1?> CreateMilestone(
        [Description("The unique identifier of the project")] Guid projectId,
        [Description("The name of the milestone")] string name,
        [Description("Optional content/description of the milestone")] string? content,
        [Description("Optional success criteria for the milestone")] string? successCriteria,
        [Description("Optional status (NotStarted, InProgress, OnHold, Completed, Cancelled)")] MilestoneStatus? status,
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
    /// Updates an existing milestone.
    /// </summary>
    [McpServerTool(Name = "milestones_update")]
    [McpTool(
        Name = "milestones_update",
        Title = "Update Milestone",
        Description = "Update an existing milestone",
        Icon = "edit")]
    public async Task<MilestoneDetailsV1?> UpdateMilestone(
        [Description("The unique identifier of the milestone to update")] Guid id,
        [Description("The new name of the milestone")] string name,
        [Description("Optional new content/description of the milestone")] string? content,
        [Description("Optional new success criteria for the milestone")] string? successCriteria,
        [Description("Optional new status (NotStarted, InProgress, OnHold, Completed, Cancelled)")] MilestoneStatus? status,
        [Description("Optional new due date for the milestone")] DateTimeOffset? dueDate,
        [Description("Optional new sort order for display")] int? sortOrder,
        CancellationToken ct)
    {
        var request = new UpdateMilestoneRequestV1
        {
            Name = name,
            Content = content,
            SuccessCriteria = successCriteria,
            Status = status ?? MilestoneStatus.NotStarted,
            DueDate = dueDate,
            SortOrder = sortOrder ?? 0
        };

        return await _milestoneService.UpdateAsync(id, request, ct);
    }

    /// <summary>
    /// Permanently deletes a milestone and all its related data.
    /// </summary>
    [McpServerTool(Name = "milestones_delete")]
    [McpTool(
        Name = "milestones_delete",
        Title = "Delete Milestone",
        Description = "Permanently delete a milestone and all its related data (todos, notes)",
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
