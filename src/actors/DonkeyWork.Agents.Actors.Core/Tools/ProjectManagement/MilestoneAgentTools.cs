using System.ComponentModel;
using DonkeyWork.Agents.Projects.Contracts.Models;
using DonkeyWork.Agents.Projects.Contracts.Services;

namespace DonkeyWork.Agents.Actors.Core.Tools.ProjectManagement;

public sealed class MilestoneAgentTools
{
    private readonly IMilestoneService _milestoneService;

    public MilestoneAgentTools(IMilestoneService milestoneService)
    {
        _milestoneService = milestoneService;
    }

    [AgentTool("milestones_list", DisplayName = "List Milestones")]
    [Description("List all milestones for a project.")]
    public async Task<ToolResult> ListMilestones(
        [Description("The project ID")] Guid projectId,
        CancellationToken ct = default)
    {
        var milestones = await _milestoneService.GetByProjectIdAsync(projectId, ct);
        return ToolResult.Json(milestones);
    }

    [AgentTool("milestones_get", DisplayName = "Get Milestone")]
    [Description("Get a milestone by ID with full details including tasks and notes.")]
    public async Task<ToolResult> GetMilestone(
        [Description("The milestone ID")] Guid milestoneId,
        [Description("Content offset for chunked reading")] int? contentOffset = null,
        [Description("Content length for chunked reading")] int? contentLength = null,
        CancellationToken ct = default)
    {
        var milestone = await _milestoneService.GetByIdAsync(milestoneId, contentOffset, contentLength, ct);
        return milestone is not null ? ToolResult.Json(milestone) : ToolResult.NotFound("Milestone", milestoneId);
    }

    [AgentTool("milestones_create", DisplayName = "Create Milestone")]
    [Description("Create a new milestone within a project.")]
    public async Task<ToolResult> CreateMilestone(
        [Description("The project ID")] Guid projectId,
        [Description("The milestone name")] string name,
        [Description("The milestone content/description")] string? content = null,
        [Description("Success criteria")] string? successCriteria = null,
        [Description("Milestone status: NotStarted, InProgress, OnHold, Completed, Cancelled")] MilestoneStatus? status = null,
        [Description("Sort order")] int sortOrder = 0,
        CancellationToken ct = default)
    {
        var request = new CreateMilestoneRequestV1
        {
            Name = name,
            Content = content,
            SuccessCriteria = successCriteria,
            Status = status ?? MilestoneStatus.NotStarted,
            SortOrder = sortOrder,
        };
        var milestone = await _milestoneService.CreateAsync(projectId, request, ct);
        return milestone is not null ? ToolResult.Json(milestone) : ToolResult.NotFound("Project", projectId);
    }

    [AgentTool("milestones_update", DisplayName = "Update Milestone")]
    [Description("Update an existing milestone.")]
    public async Task<ToolResult> UpdateMilestone(
        [Description("The milestone ID")] Guid milestoneId,
        [Description("The milestone name")] string name,
        [Description("The milestone content/description")] string? content = null,
        [Description("Success criteria")] string? successCriteria = null,
        [Description("Milestone status: NotStarted, InProgress, OnHold, Completed, Cancelled")] MilestoneStatus? status = null,
        [Description("Completion notes")] string? completionNotes = null,
        [Description("Sort order")] int sortOrder = 0,
        CancellationToken ct = default)
    {
        var request = new UpdateMilestoneRequestV1
        {
            Name = name,
            Content = content,
            SuccessCriteria = successCriteria,
            Status = status ?? MilestoneStatus.NotStarted,
            CompletionNotes = completionNotes,
            SortOrder = sortOrder,
        };
        var milestone = await _milestoneService.UpdateAsync(milestoneId, request, ct);
        return milestone is not null
            ? ToolResult.Json(new UpdateAcknowledgmentV1 { Id = milestoneId, Status = "updated" })
            : ToolResult.NotFound("Milestone", milestoneId);
    }

    [AgentTool("milestones_delete", DisplayName = "Delete Milestone")]
    [Description("Delete a milestone and all its related data.")]
    public async Task<ToolResult> DeleteMilestone(
        [Description("The milestone ID")] Guid milestoneId,
        CancellationToken ct = default)
    {
        var deleted = await _milestoneService.DeleteAsync(milestoneId, ct);
        return deleted
            ? ToolResult.Success($"Milestone '{milestoneId}' deleted successfully.")
            : ToolResult.NotFound("Milestone", milestoneId);
    }
}
