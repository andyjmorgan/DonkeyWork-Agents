using System.ComponentModel;
using DonkeyWork.Agents.Projects.Contracts.Models;
using DonkeyWork.Agents.Projects.Contracts.Services;

namespace DonkeyWork.Agents.Actors.Core.Tools.ProjectManagement;

public sealed class TaskAgentTools
{
    private readonly ITaskItemService _taskItemService;

    public TaskAgentTools(ITaskItemService taskItemService)
    {
        _taskItemService = taskItemService;
    }

    [AgentTool("tasks_list", DisplayName = "List Tasks")]
    [Description("List all task items for the current user.")]
    public async Task<ToolResult> ListTasks(CancellationToken ct = default)
    {
        var tasks = await _taskItemService.ListAsync(ct);
        return ToolResult.Json(tasks);
    }

    [AgentTool("tasks_list_by_project", DisplayName = "List Tasks by Project")]
    [Description("List all task items for a project.")]
    public async Task<ToolResult> ListTasksByProject(
        [Description("The project ID")] Guid projectId,
        CancellationToken ct = default)
    {
        var tasks = await _taskItemService.GetByProjectIdAsync(projectId, ct);
        return ToolResult.Json(tasks);
    }

    [AgentTool("tasks_list_by_milestone", DisplayName = "List Tasks by Milestone")]
    [Description("List all task items for a milestone.")]
    public async Task<ToolResult> ListTasksByMilestone(
        [Description("The milestone ID")] Guid milestoneId,
        CancellationToken ct = default)
    {
        var tasks = await _taskItemService.GetByMilestoneIdAsync(milestoneId, ct);
        return ToolResult.Json(tasks);
    }

    [AgentTool("tasks_get", DisplayName = "Get Task")]
    [Description("Get a task item by ID with full details.")]
    public async Task<ToolResult> GetTask(
        [Description("The task item ID")] Guid taskItemId,
        [Description("Content offset for chunked reading")] int? contentOffset = null,
        [Description("Content length for chunked reading")] int? contentLength = null,
        CancellationToken ct = default)
    {
        var task = await _taskItemService.GetByIdAsync(taskItemId, contentOffset, contentLength, ct);
        return task is not null ? ToolResult.Json(task) : ToolResult.NotFound("Task", taskItemId);
    }

    [AgentTool("tasks_create", DisplayName = "Create Task")]
    [Description("Create a new task item, optionally associated with a project and/or milestone.")]
    public async Task<ToolResult> CreateTask(
        [Description("The task title")] string title,
        [Description("The task description")] string? description = null,
        [Description("A brief summary")] string? summary = null,
        [Description("Task status: Pending, InProgress, Completed, Cancelled")] TaskItemStatus? status = null,
        [Description("Task priority: Low, Medium, High, Critical")] TaskItemPriority? priority = null,
        [Description("Sort order")] int sortOrder = 0,
        [Description("Associated project ID")] Guid? projectId = null,
        [Description("Associated milestone ID")] Guid? milestoneId = null,
        CancellationToken ct = default)
    {
        var request = new CreateTaskItemRequestV1
        {
            Title = title,
            Description = description,
            Summary = summary,
            Status = status ?? TaskItemStatus.Pending,
            Priority = priority ?? TaskItemPriority.Medium,
            SortOrder = sortOrder,
            ProjectId = projectId,
            MilestoneId = milestoneId,
        };
        var task = await _taskItemService.CreateAsync(request, ct);
        return ToolResult.Json(task);
    }

    [AgentTool("tasks_update", DisplayName = "Update Task")]
    [Description("Update an existing task item.")]
    public async Task<ToolResult> UpdateTask(
        [Description("The task item ID")] Guid taskItemId,
        [Description("The task title")] string title,
        [Description("The task description")] string? description = null,
        [Description("A brief summary")] string? summary = null,
        [Description("Task status: Pending, InProgress, Completed, Cancelled")] TaskItemStatus? status = null,
        [Description("Task priority: Low, Medium, High, Critical")] TaskItemPriority? priority = null,
        [Description("Completion notes")] string? completionNotes = null,
        [Description("Sort order")] int sortOrder = 0,
        [Description("Associated project ID")] Guid? projectId = null,
        [Description("Associated milestone ID")] Guid? milestoneId = null,
        CancellationToken ct = default)
    {
        var request = new UpdateTaskItemRequestV1
        {
            Title = title,
            Description = description,
            Summary = summary,
            Status = status ?? TaskItemStatus.Pending,
            Priority = priority ?? TaskItemPriority.Medium,
            CompletionNotes = completionNotes,
            SortOrder = sortOrder,
            ProjectId = projectId,
            MilestoneId = milestoneId,
        };
        var task = await _taskItemService.UpdateAsync(taskItemId, request, ct);
        return task is not null
            ? ToolResult.Json(new UpdateAcknowledgmentV1 { Id = taskItemId, Status = "updated" })
            : ToolResult.NotFound("Task", taskItemId);
    }

    [AgentTool("tasks_delete", DisplayName = "Delete Task")]
    [Description("Delete a task item.")]
    public async Task<ToolResult> DeleteTask(
        [Description("The task item ID")] Guid taskItemId,
        CancellationToken ct = default)
    {
        var deleted = await _taskItemService.DeleteAsync(taskItemId, ct);
        return deleted
            ? ToolResult.Success($"Task '{taskItemId}' deleted successfully.")
            : ToolResult.NotFound("Task", taskItemId);
    }
}
