using System.ComponentModel;
using DonkeyWork.Agents.Mcp.Contracts;
using DonkeyWork.Agents.Projects.Contracts.Models;
using DonkeyWork.Agents.Projects.Contracts.Services;
using ModelContextProtocol.Server;

namespace DonkeyWork.Agents.Projects.Api.McpTools;

/// <summary>
/// MCP tools for managing tasks.
/// </summary>
[McpServerToolType]
[McpToolProvider(Provider = McpToolProvider.DonkeyWork)]
public class TasksTools
{
    private readonly ITaskItemService _taskItemService;

    /// <summary>
    /// Initializes a new instance of the <see cref="TasksTools"/> class.
    /// </summary>
    public TasksTools(ITaskItemService taskItemService)
    {
        _taskItemService = taskItemService;
    }

    /// <summary>
    /// Lists all tasks for the current user.
    /// Returns summary models without description/completionNotes - use tasks_get for full details.
    /// </summary>
    [McpServerTool(Name = "tasks_list")]
    [McpTool(
        Name = "tasks_list",
        Title = "List Tasks",
        Description = "List all tasks for the current user. Tasks can be standalone, or optionally associated with a project or milestone. Returns summary models - use tasks_get for full task details including description.",
        Icon = "list",
        ReadOnlyHint = true)]
    public async Task<IReadOnlyList<TaskItemSummaryV1>> ListTasks(CancellationToken ct)
    {
        return await _taskItemService.ListAsync(ct);
    }

    /// <summary>
    /// Gets a task by ID.
    /// </summary>
    [McpServerTool(Name = "tasks_get")]
    [McpTool(
        Name = "tasks_get",
        Title = "Get Task",
        Description = "Get a task by ID",
        Icon = "file",
        ReadOnlyHint = true)]
    public async Task<TaskItemV1?> GetTask(
        [Description("The unique identifier of the task")] Guid id,
        [Description("Optional character offset to start reading content from (for chunked reading of large content fields)")] int? contentOffset = null,
        [Description("Optional number of characters to read from the offset (for chunked reading of large content fields)")] int? contentLength = null,
        CancellationToken ct = default)
    {
        return await _taskItemService.GetByIdAsync(id, contentOffset, contentLength, ct);
    }

    /// <summary>
    /// Creates a new task.
    /// </summary>
    [McpServerTool(Name = "tasks_create")]
    [McpTool(
        Name = "tasks_create",
        Title = "Create Task",
        Description = "Create a new task. Tasks can be standalone (no project or milestone), associated with a project only, or associated with a specific milestone within a project. Provide projectId for project-level tasks, or milestoneId for milestone-level tasks.",
        Icon = "plus")]
    public async Task<TaskItemV1> CreateTask(
        [Description("The title of the task")] string title,
        [Description("Optional description of the task (supports markdown and mermaid diagrams)")] string? description,
        [Description("Status: Pending (default), InProgress, Completed, or Cancelled")] TaskItemStatus? status,
        [Description("Priority: Low, Medium (default), High, or Critical")] TaskItemPriority? priority,
        [Description("Optional project ID - set to associate this task with a project (task becomes project-level)")] Guid? projectId,
        [Description("Optional milestone ID - set to associate this task with a milestone (task becomes milestone-level)")] Guid? milestoneId,
        CancellationToken ct)
    {
        var request = new CreateTaskItemRequestV1
        {
            Title = title,
            Description = description,
            Status = status ?? TaskItemStatus.Pending,
            Priority = priority ?? TaskItemPriority.Medium,
            ProjectId = projectId,
            MilestoneId = milestoneId
        };

        return await _taskItemService.CreateAsync(request, ct);
    }

    /// <summary>
    /// Updates an existing task. Only provided fields are updated; omitted fields retain their current values.
    /// </summary>
    [McpServerTool(Name = "tasks_update")]
    [McpTool(
        Name = "tasks_update",
        Title = "Update Task",
        Description = "Update an existing task. IMPORTANT: Only `id` is required - all other parameters are optional. Do NOT pass fields you don't intend to change; omitted fields keep their current values automatically. For example, to change only the status, pass just `id` and `status`. You can move tasks between standalone/project/milestone associations by setting projectId and milestoneId.",
        Icon = "edit")]
    public async Task<UpdateAcknowledgmentV1?> UpdateTask(
        [Description("The unique identifier of the task to update")] Guid id,
        [Description("New title for the task (omit to keep current)")] string? title = null,
        [Description("New description (supports markdown, omit to keep current)")] string? description = null,
        [Description("Status: Pending, InProgress, Completed, or Cancelled (omit to keep current)")] TaskItemStatus? status = null,
        [Description("Priority: Low, Medium, High, or Critical (omit to keep current)")] TaskItemPriority? priority = null,
        [Description("Completion notes (set when marking as Completed, omit to keep current)")] string? completionNotes = null,
        [Description("Project ID to associate with (omit to keep current)")] Guid? projectId = null,
        [Description("Milestone ID to associate with (omit to keep current)")] Guid? milestoneId = null,
        CancellationToken ct = default)
    {
        // Fetch current task to merge with provided values
        var current = await _taskItemService.GetByIdAsync(id, cancellationToken: ct);
        if (current == null)
        {
            return null;
        }

        var request = new UpdateTaskItemRequestV1
        {
            Title = title ?? current.Title,
            Description = description ?? current.Description,
            Status = status ?? current.Status,
            Priority = priority ?? current.Priority,
            CompletionNotes = completionNotes ?? current.CompletionNotes,
            ProjectId = projectId ?? current.ProjectId,
            MilestoneId = milestoneId ?? current.MilestoneId
        };

        var updated = await _taskItemService.UpdateAsync(id, request, ct);
        if (updated == null)
        {
            return null;
        }

        return new UpdateAcknowledgmentV1 { Id = id, Status = "updated" };
    }

    /// <summary>
    /// Permanently deletes a task.
    /// </summary>
    [McpServerTool(Name = "tasks_delete")]
    [McpTool(
        Name = "tasks_delete",
        Title = "Delete Task",
        Description = "Permanently delete a task",
        Icon = "trash",
        DestructiveHint = true,
        IdempotentHint = true)]
    public async Task<bool> DeleteTask(
        [Description("The unique identifier of the task to delete")] Guid id,
        CancellationToken ct)
    {
        return await _taskItemService.DeleteAsync(id, ct);
    }

    /// <summary>
    /// Lists all tasks for a specific project.
    /// Returns summary models without description/completionNotes - use tasks_get for full details.
    /// </summary>
    [McpServerTool(Name = "tasks_list_by_project")]
    [McpTool(
        Name = "tasks_list_by_project",
        Title = "List Tasks by Project",
        Description = "List all tasks directly associated with a specific project (project-level tasks only). Does not include tasks associated with milestones within the project - use tasks_list_by_milestone for those. Returns summary models - use tasks_get for full task details.",
        Icon = "list",
        ReadOnlyHint = true)]
    public async Task<IReadOnlyList<TaskItemSummaryV1>> ListTasksByProject(
        [Description("The unique identifier of the project")] Guid projectId,
        CancellationToken ct)
    {
        return await _taskItemService.GetByProjectIdAsync(projectId, ct);
    }

    /// <summary>
    /// Lists all tasks for a specific milestone.
    /// Returns summary models without description/completionNotes - use tasks_get for full details.
    /// </summary>
    [McpServerTool(Name = "tasks_list_by_milestone")]
    [McpTool(
        Name = "tasks_list_by_milestone",
        Title = "List Tasks by Milestone",
        Description = "List all tasks associated with a specific milestone. Milestones belong to projects and represent major deliverables or phases. Returns summary models - use tasks_get for full task details.",
        Icon = "list",
        ReadOnlyHint = true)]
    public async Task<IReadOnlyList<TaskItemSummaryV1>> ListTasksByMilestone(
        [Description("The unique identifier of the milestone")] Guid milestoneId,
        CancellationToken ct)
    {
        return await _taskItemService.GetByMilestoneIdAsync(milestoneId, ct);
    }
}
