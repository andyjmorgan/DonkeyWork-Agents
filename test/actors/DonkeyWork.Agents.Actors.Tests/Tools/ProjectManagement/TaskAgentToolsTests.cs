using System.Text.Json;
using DonkeyWork.Agents.Actors.Core.Tools.ProjectManagement;
using DonkeyWork.Agents.Common.Contracts.Models.Pagination;
using DonkeyWork.Agents.Projects.Contracts.Models;
using DonkeyWork.Agents.Projects.Contracts.Services;
using Moq;
using Xunit;

namespace DonkeyWork.Agents.Actors.Tests.Tools.ProjectManagement;

public class TaskAgentToolsTests
{
    private readonly Mock<ITaskItemService> _taskItemService = new();
    private readonly TaskAgentTools _tools;

    public TaskAgentToolsTests()
    {
        _tools = new TaskAgentTools(_taskItemService.Object);
    }

    #region ListTasks Tests

    [Fact]
    public async Task ListTasks_ReturnsJsonResult()
    {
        // Arrange
        var tasks = new PaginatedResponse<TaskItemSummaryV1>
        {
            Items = new List<TaskItemSummaryV1>
            {
                new() { Id = Guid.NewGuid(), Title = "Task 1" },
            },
            Offset = 0,
            Limit = 50,
            TotalCount = 1
        };
        _taskItemService.Setup(x => x.ListAsync(It.IsAny<PaginationRequest>(), It.IsAny<TaskItemFilterRequestV1>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tasks);

        // Act
        var result = await _tools.ListTasks();

        // Assert
        Assert.False(result.IsError);
    }

    #endregion

    #region ListTasksByProject Tests

    [Fact]
    public async Task ListTasksByProject_DelegatesToService()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        _taskItemService.Setup(x => x.GetByProjectIdAsync(projectId, It.IsAny<PaginationRequest>(), It.IsAny<TaskItemFilterRequestV1>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaginatedResponse<TaskItemSummaryV1> { Items = [], Offset = 0, Limit = 50, TotalCount = 0 });

        // Act
        var result = await _tools.ListTasksByProject(projectId);

        // Assert
        Assert.False(result.IsError);
        _taskItemService.Verify(x => x.GetByProjectIdAsync(projectId, It.IsAny<PaginationRequest>(), It.IsAny<TaskItemFilterRequestV1>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region ListTasksByMilestone Tests

    [Fact]
    public async Task ListTasksByMilestone_DelegatesToService()
    {
        // Arrange
        var milestoneId = Guid.NewGuid();
        _taskItemService.Setup(x => x.GetByMilestoneIdAsync(milestoneId, It.IsAny<PaginationRequest>(), It.IsAny<TaskItemFilterRequestV1>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaginatedResponse<TaskItemSummaryV1> { Items = [], Offset = 0, Limit = 50, TotalCount = 0 });

        // Act
        var result = await _tools.ListTasksByMilestone(milestoneId);

        // Assert
        Assert.False(result.IsError);
        _taskItemService.Verify(x => x.GetByMilestoneIdAsync(milestoneId, It.IsAny<PaginationRequest>(), It.IsAny<TaskItemFilterRequestV1>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetTask Tests

    [Fact]
    public async Task GetTask_WhenFound_ReturnsJsonResult()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var task = new TaskItemV1 { Id = taskId, Title = "Test Task" };
        _taskItemService.Setup(x => x.GetByIdAsync(taskId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        // Act
        var result = await _tools.GetTask(taskId);

        // Assert
        Assert.False(result.IsError);
        Assert.Contains("Test Task", result.Content);
    }

    [Fact]
    public async Task GetTask_WhenNotFound_ReturnsNotFoundError()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        _taskItemService.Setup(x => x.GetByIdAsync(taskId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TaskItemV1?)null);

        // Act
        var result = await _tools.GetTask(taskId);

        // Assert
        Assert.True(result.IsError);
        Assert.Contains("not found", result.Content);
    }

    #endregion

    #region CreateTask Tests

    [Fact]
    public async Task CreateTask_DelegatesToService()
    {
        // Arrange
        var created = new TaskItemV1 { Id = Guid.NewGuid(), Title = "New Task" };
        _taskItemService.Setup(x => x.CreateAsync(It.IsAny<CreateTaskItemRequestV1>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(created);

        // Act
        var result = await _tools.CreateTask("New Task", "Description", priority: TaskItemPriority.High);

        // Assert
        Assert.False(result.IsError);
        _taskItemService.Verify(x => x.CreateAsync(
            It.Is<CreateTaskItemRequestV1>(r => r.Title == "New Task" && r.Description == "Description" && r.Priority == TaskItemPriority.High),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region UpdateTask Tests

    [Fact]
    public async Task UpdateTask_WhenFound_ReturnsJsonResult()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var updated = new TaskItemV1 { Id = taskId, Title = "Updated Task" };
        _taskItemService.Setup(x => x.UpdateAsync(taskId, It.IsAny<UpdateTaskItemRequestV1>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updated);

        // Act
        var result = await _tools.UpdateTask(taskId, "Updated Task", status: TaskItemStatus.Completed);

        // Assert
        Assert.False(result.IsError);
        _taskItemService.Verify(x => x.UpdateAsync(
            taskId,
            It.Is<UpdateTaskItemRequestV1>(r => r.Title == "Updated Task" && r.Status == TaskItemStatus.Completed),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateTask_WhenNotFound_ReturnsNotFoundError()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        _taskItemService.Setup(x => x.UpdateAsync(taskId, It.IsAny<UpdateTaskItemRequestV1>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TaskItemV1?)null);

        // Act
        var result = await _tools.UpdateTask(taskId, "Updated");

        // Assert
        Assert.True(result.IsError);
        Assert.Contains("not found", result.Content);
    }

    #endregion

    #region DeleteTask Tests

    [Fact]
    public async Task DeleteTask_WhenDeleted_ReturnsSuccess()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        _taskItemService.Setup(x => x.DeleteAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _tools.DeleteTask(taskId);

        // Assert
        Assert.False(result.IsError);
        Assert.Contains("deleted successfully", result.Content);
    }

    [Fact]
    public async Task DeleteTask_WhenNotFound_ReturnsNotFoundError()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        _taskItemService.Setup(x => x.DeleteAsync(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _tools.DeleteTask(taskId);

        // Assert
        Assert.True(result.IsError);
        Assert.Contains("not found", result.Content);
    }

    #endregion
}
