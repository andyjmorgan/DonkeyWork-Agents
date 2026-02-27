using System.Text.Json;
using DonkeyWork.Agents.Actors.Core.Tools.ProjectManagement;
using DonkeyWork.Agents.Projects.Contracts.Models;
using DonkeyWork.Agents.Projects.Contracts.Services;
using Moq;
using Xunit;

namespace DonkeyWork.Agents.Actors.Tests.Tools.ProjectManagement;

public class ProjectAgentToolsTests
{
    private readonly Mock<IProjectService> _projectService = new();
    private readonly ProjectAgentTools _tools;

    public ProjectAgentToolsTests()
    {
        _tools = new ProjectAgentTools(_projectService.Object);
    }

    #region ListProjects Tests

    [Fact]
    public async Task ListProjects_ReturnsJsonResult()
    {
        // Arrange
        var projects = new List<ProjectSummaryV1>
        {
            new() { Id = Guid.NewGuid(), Name = "Project 1", Status = ProjectStatus.InProgress },
        };
        _projectService.Setup(x => x.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(projects);

        // Act
        var result = await _tools.ListProjects(CancellationToken.None);

        // Assert
        Assert.False(result.IsError);
        var deserialized = JsonSerializer.Deserialize<List<JsonElement>>(result.Content);
        Assert.NotNull(deserialized);
        Assert.Single(deserialized);
    }

    #endregion

    #region GetProject Tests

    [Fact]
    public async Task GetProject_WhenFound_ReturnsJsonResult()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var project = new ProjectDetailsV1 { Id = projectId, Name = "Test" };
        _projectService.Setup(x => x.GetByIdAsync(projectId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        // Act
        var result = await _tools.GetProject(projectId);

        // Assert
        Assert.False(result.IsError);
        Assert.Contains("Test", result.Content);
    }

    [Fact]
    public async Task GetProject_WhenNotFound_ReturnsNotFoundError()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        _projectService.Setup(x => x.GetByIdAsync(projectId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectDetailsV1?)null);

        // Act
        var result = await _tools.GetProject(projectId);

        // Assert
        Assert.True(result.IsError);
        Assert.Contains("not found", result.Content);
    }

    #endregion

    #region CreateProject Tests

    [Fact]
    public async Task CreateProject_DelegatesToService()
    {
        // Arrange
        var created = new ProjectDetailsV1 { Id = Guid.NewGuid(), Name = "New Project" };
        _projectService.Setup(x => x.CreateAsync(It.IsAny<CreateProjectRequestV1>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(created);

        // Act
        var result = await _tools.CreateProject("New Project", "Description", ProjectStatus.InProgress);

        // Assert
        Assert.False(result.IsError);
        _projectService.Verify(x => x.CreateAsync(
            It.Is<CreateProjectRequestV1>(r => r.Name == "New Project" && r.Content == "Description" && r.Status == ProjectStatus.InProgress),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region UpdateProject Tests

    [Fact]
    public async Task UpdateProject_WhenFound_ReturnsJsonResult()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var updated = new ProjectDetailsV1 { Id = projectId, Name = "Updated" };
        _projectService.Setup(x => x.UpdateAsync(projectId, It.IsAny<UpdateProjectRequestV1>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updated);

        // Act
        var result = await _tools.UpdateProject(projectId, "Updated");

        // Assert
        Assert.False(result.IsError);
        Assert.Contains("Updated", result.Content);
    }

    [Fact]
    public async Task UpdateProject_WhenNotFound_ReturnsNotFoundError()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        _projectService.Setup(x => x.UpdateAsync(projectId, It.IsAny<UpdateProjectRequestV1>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectDetailsV1?)null);

        // Act
        var result = await _tools.UpdateProject(projectId, "Updated");

        // Assert
        Assert.True(result.IsError);
        Assert.Contains("not found", result.Content);
    }

    #endregion

    #region DeleteProject Tests

    [Fact]
    public async Task DeleteProject_WhenDeleted_ReturnsSuccess()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        _projectService.Setup(x => x.DeleteAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _tools.DeleteProject(projectId);

        // Assert
        Assert.False(result.IsError);
        Assert.Contains("deleted successfully", result.Content);
    }

    [Fact]
    public async Task DeleteProject_WhenNotFound_ReturnsNotFoundError()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        _projectService.Setup(x => x.DeleteAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _tools.DeleteProject(projectId);

        // Assert
        Assert.True(result.IsError);
        Assert.Contains("not found", result.Content);
    }

    #endregion
}
