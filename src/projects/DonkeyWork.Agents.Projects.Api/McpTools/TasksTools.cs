using System.ComponentModel;
using DonkeyWork.Agents.Mcp.Contracts;
using DonkeyWork.Agents.Projects.Contracts.Models;
using DonkeyWork.Agents.Projects.Contracts.Services;
using ModelContextProtocol.Server;

namespace DonkeyWork.Agents.Projects.Api.McpTools;

/// <summary>
/// MCP tools for managing tasks (todos).
/// </summary>
[McpServerToolType]
[McpToolProvider(Provider = McpToolProvider.DonkeyWork)]
public class TasksTools
{
    private readonly ITodoService _todoService;

    /// <summary>
    /// Initializes a new instance of the <see cref="TasksTools"/> class.
    /// </summary>
    public TasksTools(ITodoService todoService)
    {
        _todoService = todoService;
    }

    /// <summary>
    /// Lists all tasks for the current user.
    /// </summary>
    [McpServerTool(Name = "tasks_list")]
    [McpTool(
        Name = "tasks_list",
        Title = "List Tasks",
        Description = "List all tasks (todos) for the current user. Tasks can be standalone, or optionally associated with a project or milestone. Returns all tasks regardless of their association.",
        Icon = "list",
        ReadOnlyHint = true)]
    public async Task<IReadOnlyList<TodoV1>> ListTasks(CancellationToken ct)
    {
        return await _todoService.ListAsync(ct);
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
    public async Task<TodoV1?> GetTask(
        [Description("The unique identifier of the task")] Guid id,
        CancellationToken ct)
    {
        return await _todoService.GetByIdAsync(id, ct);
    }

    /// <summary>
    /// Creates a new task.
    /// </summary>
    [McpServerTool(Name = "tasks_create")]
    [McpTool(
        Name = "tasks_create",
        Title = "Create Task",
        Description = "Create a new task (todo). Tasks can be standalone (no project or milestone), associated with a project only, or associated with a specific milestone within a project. Provide projectId for project-level tasks, or milestoneId for milestone-level tasks.",
        Icon = "plus")]
    public async Task<TodoV1> CreateTask(
        [Description("The title of the task")] string title,
        [Description("Optional description of the task (supports markdown and mermaid diagrams)")] string? description,
        [Description("Status: Pending (default), InProgress, Completed, or Cancelled")] TodoStatus? status,
        [Description("Priority: Low, Medium (default), High, or Critical")] TodoPriority? priority,
        [Description("Optional due date for the task")] DateTimeOffset? dueDate,
        [Description("Optional project ID - set to associate this task with a project (task becomes project-level)")] Guid? projectId,
        [Description("Optional milestone ID - set to associate this task with a milestone (task becomes milestone-level)")] Guid? milestoneId,
        CancellationToken ct)
    {
        var request = new CreateTodoRequestV1
        {
            Title = title,
            Description = description,
            Status = status ?? TodoStatus.Pending,
            Priority = priority ?? TodoPriority.Medium,
            DueDate = dueDate,
            ProjectId = projectId,
            MilestoneId = milestoneId
        };

        return await _todoService.CreateAsync(request, ct);
    }

    /// <summary>
    /// Updates an existing task. Only provided fields are updated; omitted fields retain their current values.
    /// </summary>
    [McpServerTool(Name = "tasks_update")]
    [McpTool(
        Name = "tasks_update",
        Title = "Update Task",
        Description = "Update an existing task. Only provided fields are updated - omit fields to keep their current values. You can move tasks between standalone/project/milestone associations by setting projectId and milestoneId.",
        Icon = "edit")]
    public async Task<TodoV1?> UpdateTask(
        [Description("The unique identifier of the task to update")] Guid id,
        [Description("New title for the task (omit to keep current)")] string? title = null,
        [Description("New description (supports markdown, omit to keep current)")] string? description = null,
        [Description("Status: Pending, InProgress, Completed, or Cancelled (omit to keep current)")] TodoStatus? status = null,
        [Description("Priority: Low, Medium, High, or Critical (omit to keep current)")] TodoPriority? priority = null,
        [Description("Completion notes (set when marking as Completed, omit to keep current)")] string? completionNotes = null,
        [Description("New due date (omit to keep current)")] DateTimeOffset? dueDate = null,
        [Description("Project ID to associate with (omit to keep current)")] Guid? projectId = null,
        [Description("Milestone ID to associate with (omit to keep current)")] Guid? milestoneId = null,
        CancellationToken ct = default)
    {
        // Fetch current task to merge with provided values
        var current = await _todoService.GetByIdAsync(id, ct);
        if (current == null)
        {
            return null;
        }

        var request = new UpdateTodoRequestV1
        {
            Title = title ?? current.Title,
            Description = description ?? current.Description,
            Status = status ?? current.Status,
            Priority = priority ?? current.Priority,
            CompletionNotes = completionNotes ?? current.CompletionNotes,
            DueDate = dueDate ?? current.DueDate,
            ProjectId = projectId ?? current.ProjectId,
            MilestoneId = milestoneId ?? current.MilestoneId
        };

        return await _todoService.UpdateAsync(id, request, ct);
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
        return await _todoService.DeleteAsync(id, ct);
    }

    /// <summary>
    /// Lists all tasks for a specific project.
    /// </summary>
    [McpServerTool(Name = "tasks_list_by_project")]
    [McpTool(
        Name = "tasks_list_by_project",
        Title = "List Tasks by Project",
        Description = "List all tasks directly associated with a specific project (project-level tasks only). Does not include tasks associated with milestones within the project - use tasks_list_by_milestone for those.",
        Icon = "list",
        ReadOnlyHint = true)]
    public async Task<IReadOnlyList<TodoV1>> ListTasksByProject(
        [Description("The unique identifier of the project")] Guid projectId,
        CancellationToken ct)
    {
        return await _todoService.GetByProjectIdAsync(projectId, ct);
    }

    /// <summary>
    /// Lists all tasks for a specific milestone.
    /// </summary>
    [McpServerTool(Name = "tasks_list_by_milestone")]
    [McpTool(
        Name = "tasks_list_by_milestone",
        Title = "List Tasks by Milestone",
        Description = "List all tasks associated with a specific milestone. Milestones belong to projects and represent major deliverables or phases.",
        Icon = "list",
        ReadOnlyHint = true)]
    public async Task<IReadOnlyList<TodoV1>> ListTasksByMilestone(
        [Description("The unique identifier of the milestone")] Guid milestoneId,
        CancellationToken ct)
    {
        return await _todoService.GetByMilestoneIdAsync(milestoneId, ct);
    }
}
