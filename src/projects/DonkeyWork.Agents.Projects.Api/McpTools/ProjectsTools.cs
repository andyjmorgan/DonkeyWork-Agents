using System.ComponentModel;
using DonkeyWork.Agents.Mcp.Contracts;
using DonkeyWork.Agents.Projects.Contracts.Models;
using DonkeyWork.Agents.Projects.Contracts.Services;
using ModelContextProtocol.Server;

namespace DonkeyWork.Agents.Projects.Api.McpTools;

/// <summary>
/// MCP tools for managing projects.
/// </summary>
[McpServerToolType]
[McpToolProvider(Provider = McpToolProvider.DonkeyWork)]
public class ProjectsTools
{
    private readonly IProjectService _projectService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectsTools"/> class.
    /// </summary>
    public ProjectsTools(IProjectService projectService)
    {
        _projectService = projectService;
    }

    /// <summary>
    /// Lists all projects for the current user.
    /// </summary>
    [McpServerTool(Name = "projects_list")]
    [McpTool(
        Name = "projects_list",
        Title = "List Projects",
        Description = "List all projects for the current user. Projects are the top-level container and can contain milestones, tasks, and notes. Tasks and notes can also exist standalone or at the project level.",
        Icon = "list",
        ReadOnlyHint = true)]
    public async Task<IReadOnlyList<ProjectSummaryV1>> ListProjects(CancellationToken ct)
    {
        return await _projectService.ListAsync(ct);
    }

    /// <summary>
    /// Gets a project by ID with full details.
    /// </summary>
    [McpServerTool(Name = "projects_get")]
    [McpTool(
        Name = "projects_get",
        Title = "Get Project",
        Description = "Get a project by ID with full details including its milestones, project-level tasks, and project-level notes. Use milestones_get to see tasks/notes within a specific milestone.",
        Icon = "file",
        ReadOnlyHint = true)]
    public async Task<ProjectDetailsV1?> GetProject(
        [Description("The unique identifier of the project")] Guid id,
        CancellationToken ct)
    {
        return await _projectService.GetByIdAsync(id, ct);
    }

    /// <summary>
    /// Creates a new project.
    /// </summary>
    [McpServerTool(Name = "projects_create")]
    [McpTool(
        Name = "projects_create",
        Title = "Create Project",
        Description = "Create a new project. Projects are the top-level container that can hold milestones, tasks, and notes. After creating a project, you can add milestones with milestones_create, or add tasks/notes directly to the project.",
        Icon = "plus")]
    public async Task<ProjectDetailsV1> CreateProject(
        [Description("The name of the project")] string name,
        [Description("Optional content/description of the project (supports markdown and mermaid diagrams)")] string? content,
        [Description("Optional success criteria for the project")] string? successCriteria,
        [Description("Status: NotStarted (default), InProgress, OnHold, Completed, or Cancelled")] ProjectStatus? status,
        CancellationToken ct)
    {
        var request = new CreateProjectRequestV1
        {
            Name = name,
            Content = content,
            SuccessCriteria = successCriteria,
            Status = status ?? ProjectStatus.NotStarted
        };

        return await _projectService.CreateAsync(request, ct);
    }

    /// <summary>
    /// Updates an existing project.
    /// </summary>
    [McpServerTool(Name = "projects_update")]
    [McpTool(
        Name = "projects_update",
        Title = "Update Project",
        Description = "Update an existing project's details. Does not affect milestones, tasks, or notes within the project - use the respective tools to manage those.",
        Icon = "edit")]
    public async Task<ProjectDetailsV1?> UpdateProject(
        [Description("The unique identifier of the project to update")] Guid id,
        [Description("The new name of the project")] string name,
        [Description("Optional new content/description of the project (supports markdown and mermaid diagrams)")] string? content,
        [Description("Optional new success criteria for the project")] string? successCriteria,
        [Description("Status: NotStarted, InProgress, OnHold, Completed, or Cancelled")] ProjectStatus? status,
        CancellationToken ct)
    {
        var request = new UpdateProjectRequestV1
        {
            Name = name,
            Content = content,
            SuccessCriteria = successCriteria,
            Status = status ?? ProjectStatus.NotStarted
        };

        return await _projectService.UpdateAsync(id, request, ct);
    }

    /// <summary>
    /// Permanently deletes a project and all its related data.
    /// </summary>
    [McpServerTool(Name = "projects_delete")]
    [McpTool(
        Name = "projects_delete",
        Title = "Delete Project",
        Description = "Permanently delete a project and all its related data (milestones, todos, notes)",
        Icon = "trash",
        DestructiveHint = true,
        IdempotentHint = true)]
    public async Task<bool> DeleteProject(
        [Description("The unique identifier of the project to delete")] Guid id,
        CancellationToken ct)
    {
        return await _projectService.DeleteAsync(id, ct);
    }
}
