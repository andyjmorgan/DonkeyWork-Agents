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
        Description = "List all tasks for the current user",
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
        Description = "Create a new task",
        Icon = "plus")]
    public async Task<TodoV1> CreateTask(
        [Description("The title of the task")] string title,
        [Description("Optional description of the task")] string? description,
        [Description("Optional status (Pending, InProgress, Completed, Cancelled)")] TodoStatus? status,
        [Description("Optional priority (Low, Medium, High, Critical)")] TodoPriority? priority,
        [Description("Optional due date for the task")] DateTimeOffset? dueDate,
        [Description("Optional project ID to associate the task with")] Guid? projectId,
        [Description("Optional milestone ID to associate the task with")] Guid? milestoneId,
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
    /// Updates an existing task.
    /// </summary>
    [McpServerTool(Name = "tasks_update")]
    [McpTool(
        Name = "tasks_update",
        Title = "Update Task",
        Description = "Update an existing task",
        Icon = "edit")]
    public async Task<TodoV1?> UpdateTask(
        [Description("The unique identifier of the task to update")] Guid id,
        [Description("The new title of the task")] string title,
        [Description("Optional new description of the task")] string? description,
        [Description("Optional new status (Pending, InProgress, Completed, Cancelled)")] TodoStatus? status,
        [Description("Optional new priority (Low, Medium, High, Critical)")] TodoPriority? priority,
        [Description("Optional completion notes")] string? completionNotes,
        [Description("Optional new due date for the task")] DateTimeOffset? dueDate,
        [Description("Optional project ID to associate the task with")] Guid? projectId,
        [Description("Optional milestone ID to associate the task with")] Guid? milestoneId,
        CancellationToken ct)
    {
        var request = new UpdateTodoRequestV1
        {
            Title = title,
            Description = description,
            Status = status ?? TodoStatus.Pending,
            Priority = priority ?? TodoPriority.Medium,
            CompletionNotes = completionNotes,
            DueDate = dueDate,
            ProjectId = projectId,
            MilestoneId = milestoneId
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
        Description = "List all tasks for a specific project",
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
        Description = "List all tasks for a specific milestone",
        Icon = "list",
        ReadOnlyHint = true)]
    public async Task<IReadOnlyList<TodoV1>> ListTasksByMilestone(
        [Description("The unique identifier of the milestone")] Guid milestoneId,
        CancellationToken ct)
    {
        return await _todoService.GetByMilestoneIdAsync(milestoneId, ct);
    }
}
