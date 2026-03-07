using System.ComponentModel;
using DonkeyWork.Agents.Projects.Contracts.Models;
using DonkeyWork.Agents.Projects.Contracts.Services;

namespace DonkeyWork.Agents.Actors.Core.Tools.ProjectManagement;

public sealed class ProjectAgentTools
{
    private readonly IProjectService _projectService;

    public ProjectAgentTools(IProjectService projectService)
    {
        _projectService = projectService;
    }

    [AgentTool("projects_list", DisplayName = "List Projects")]
    [Description("List all projects for the current user.")]
    public async Task<ToolResult> ListProjects(CancellationToken ct)
    {
        var projects = await _projectService.ListAsync(ct);
        return ToolResult.Json(projects);
    }

    [AgentTool("projects_get", DisplayName = "Get Project")]
    [Description("Get a project by ID with full details including milestones, tasks, and notes.")]
    public async Task<ToolResult> GetProject(
        [Description("The project ID")] Guid projectId,
        [Description("Content offset for chunked reading")] int? contentOffset = null,
        [Description("Content length for chunked reading")] int? contentLength = null,
        CancellationToken ct = default)
    {
        var project = await _projectService.GetByIdAsync(projectId, contentOffset, contentLength, ct);
        return project is not null ? ToolResult.Json(project) : ToolResult.NotFound("Project", projectId);
    }

    [AgentTool("projects_create", DisplayName = "Create Project")]
    [Description("Create a new project.")]
    public async Task<ToolResult> CreateProject(
        [Description("The project name")] string name,
        [Description("The project content/description")] string? content = null,
        [Description("Project status: NotStarted, InProgress, OnHold, Completed, Cancelled")] ProjectStatus? status = null,
        CancellationToken ct = default)
    {
        var request = new CreateProjectRequestV1
        {
            Name = name,
            Content = content,
            Status = status ?? ProjectStatus.NotStarted,
        };
        var project = await _projectService.CreateAsync(request, ct);
        return ToolResult.Json(project);
    }

    [AgentTool("projects_update", DisplayName = "Update Project")]
    [Description("Update an existing project.")]
    public async Task<ToolResult> UpdateProject(
        [Description("The project ID")] Guid projectId,
        [Description("The project name")] string name,
        [Description("The project content/description")] string? content = null,
        [Description("Project status: NotStarted, InProgress, OnHold, Completed, Cancelled")] ProjectStatus? status = null,
        [Description("Completion notes")] string? completionNotes = null,
        CancellationToken ct = default)
    {
        var request = new UpdateProjectRequestV1
        {
            Name = name,
            Content = content,
            Status = status ?? ProjectStatus.NotStarted,
            CompletionNotes = completionNotes,
        };
        var project = await _projectService.UpdateAsync(projectId, request, ct);
        return project is not null
            ? ToolResult.Json(new UpdateAcknowledgmentV1 { Id = projectId, Status = "updated" })
            : ToolResult.NotFound("Project", projectId);
    }

    [AgentTool("projects_delete", DisplayName = "Delete Project")]
    [Description("Delete a project and all its related data.")]
    public async Task<ToolResult> DeleteProject(
        [Description("The project ID")] Guid projectId,
        CancellationToken ct = default)
    {
        var deleted = await _projectService.DeleteAsync(projectId, ct);
        return deleted
            ? ToolResult.Success($"Project '{projectId}' deleted successfully.")
            : ToolResult.NotFound("Project", projectId);
    }
}
