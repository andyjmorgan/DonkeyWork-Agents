using DonkeyWork.Agents.Identity.Contracts.Services;
using DonkeyWork.Agents.Notifications.Contracts.Interfaces;
using DonkeyWork.Agents.Persistence;
using DonkeyWork.Agents.Projects.Contracts.Models;
using DonkeyWork.Agents.Projects.Core.Services;
using DonkeyWork.Agents.Projects.Core.Tests.Helpers;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Projects.Core.Tests.Services;

/// <summary>
/// Unit tests for TaskItemService.
/// Tests CRUD operations and business logic without external dependencies.
/// </summary>
public class TaskItemServiceTests : IDisposable
{
    private readonly AgentsDbContext _dbContext;
    private readonly IIdentityContext _identityContext;
    private readonly TaskItemService _service;
    private readonly Guid _testUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly TestDataBuilder _builder = new();

    public TaskItemServiceTests()
    {
        (_dbContext, _identityContext) = MockDbContext.CreateWithIdentityContext();
        var notificationService = new Mock<INotificationService>();
        var logger = new Mock<ILogger<TaskItemService>>();
        _service = new TaskItemService(_dbContext, _identityContext, notificationService.Object, logger.Object);
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
    }

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidRequest_CreatesTaskItem()
    {
        // Arrange
        var request = new CreateTaskItemRequestV1
        {
            Title = "test-task-item",
            Description = "Test description",
            Priority = TaskItemPriority.High
        };

        // Act
        var result = await _service.CreateAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(request.Title, result.Title);
        Assert.Equal(request.Description, result.Description);
        Assert.Equal(request.Priority, result.Priority);
        Assert.Equal(TaskItemStatus.Pending, result.Status);

        // Verify task item was created in database
        var taskItemInDb = await _dbContext.TaskItems.FindAsync(result.Id);
        Assert.NotNull(taskItemInDb);
        Assert.Equal(_testUserId, taskItemInDb.UserId);
    }

    [Fact]
    public async Task CreateAsync_StandaloneTaskItem_HasNullProjectAndMilestone()
    {
        // Arrange
        var request = TestDataBuilder.CreateTaskItemRequest();

        // Act
        var result = await _service.CreateAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.ProjectId);
        Assert.Null(result.MilestoneId);
    }

    [Fact]
    public async Task CreateAsync_WithProject_AssociatesWithProject()
    {
        // Arrange
        var project = _builder.CreateProjectEntity();
        MockDbContext.SeedProject(_dbContext, project);

        var request = new CreateTaskItemRequestV1
        {
            Title = "test-task-item",
            ProjectId = project.Id
        };

        // Act
        var result = await _service.CreateAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(project.Id, result.ProjectId);
        Assert.Null(result.MilestoneId);
    }

    [Fact]
    public async Task CreateAsync_WithMilestone_AssociatesWithMilestone()
    {
        // Arrange
        var project = _builder.CreateProjectEntity();
        var milestone = _builder.CreateMilestoneEntity(projectId: project.Id);
        _dbContext.Projects.Add(project);
        MockDbContext.SeedMilestone(_dbContext, milestone);

        var request = new CreateTaskItemRequestV1
        {
            Title = "test-task-item",
            MilestoneId = milestone.Id
        };

        // Act
        var result = await _service.CreateAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(milestone.Id, result.MilestoneId);
    }

    [Fact]
    public async Task CreateAsync_WithTags_CreatesTaskItemWithTags()
    {
        // Arrange
        var request = new CreateTaskItemRequestV1
        {
            Title = "test-task-item",
            Tags = new List<TagRequestV1>
            {
                new() { Name = "urgent", Color = "#ff0000" },
                new() { Name = "backend", Color = "#00ff00" }
            }
        };

        // Act
        var result = await _service.CreateAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Tags.Count);
        Assert.Contains(result.Tags, t => t.Name == "urgent");
        Assert.Contains(result.Tags, t => t.Name == "backend");
    }

    [Fact]
    public async Task CreateAsync_WithDifferentPriorities_SetsCorrectPriority()
    {
        // Arrange & Act & Assert
        foreach (var priority in Enum.GetValues<TaskItemPriority>())
        {
            var request = new CreateTaskItemRequestV1
            {
                Title = $"task-item-{priority}",
                Priority = priority
            };

            var result = await _service.CreateAsync(request);

            Assert.NotNull(result);
            Assert.Equal(priority, result.Priority);
        }
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithExistingTaskItem_ReturnsTaskItem()
    {
        // Arrange
        var taskItem = _builder.CreateTaskItemEntity();
        MockDbContext.SeedTaskItem(_dbContext, taskItem);

        // Act
        var result = await _service.GetByIdAsync(taskItem.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(taskItem.Id, result.Id);
        Assert.Equal(taskItem.Title, result.Title);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentTaskItem_ReturnsNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _service.GetByIdAsync(nonExistentId);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region ListAsync Tests

    [Fact]
    public async Task ListAsync_WithMultipleTaskItems_ReturnsAllUserTaskItems()
    {
        // Arrange
        var taskItem1 = _builder.CreateTaskItemEntity(title: "task-item-1");
        var taskItem2 = _builder.CreateTaskItemEntity(title: "task-item-2");
        var taskItem3 = _builder.CreateTaskItemEntity(title: "task-item-3");
        _dbContext.TaskItems.AddRange(taskItem1, taskItem2, taskItem3);
        await _dbContext.SaveChangesAsync();

        // Act
        var results = await _service.ListAsync();

        // Assert
        Assert.NotNull(results);
        Assert.Equal(3, results.Count);
    }

    [Fact]
    public async Task ListAsync_WithNoTaskItems_ReturnsEmptyList()
    {
        // Act
        var results = await _service.ListAsync();

        // Assert
        Assert.NotNull(results);
        Assert.Empty(results);
    }

    #endregion

    #region GetStandaloneAsync Tests

    [Fact]
    public async Task GetStandaloneAsync_ReturnsOnlyStandaloneTaskItems()
    {
        // Arrange
        var project = _builder.CreateProjectEntity();
        MockDbContext.SeedProject(_dbContext, project);

        var standaloneTaskItem = _builder.CreateTaskItemEntity(title: "standalone");
        var projectTaskItem = _builder.CreateTaskItemEntity(title: "project-task-item", projectId: project.Id);

        _dbContext.TaskItems.AddRange(standaloneTaskItem, projectTaskItem);
        await _dbContext.SaveChangesAsync();

        // Act
        var results = await _service.GetStandaloneAsync();

        // Assert
        Assert.NotNull(results);
        Assert.Single(results);
        Assert.Equal("standalone", results[0].Title);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidRequest_UpdatesTaskItem()
    {
        // Arrange
        var taskItem = _builder.CreateTaskItemEntity();
        MockDbContext.SeedTaskItem(_dbContext, taskItem);

        var updateRequest = new UpdateTaskItemRequestV1
        {
            Title = "updated-task-item",
            Description = "Updated description",
            Status = TaskItemStatus.InProgress,
            Priority = TaskItemPriority.Critical,
            SortOrder = 10
        };

        // Act
        var result = await _service.UpdateAsync(taskItem.Id, updateRequest);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(taskItem.Id, result.Id);
        Assert.Equal(updateRequest.Title, result.Title);
        Assert.Equal(updateRequest.Description, result.Description);
        Assert.Equal(updateRequest.Status, result.Status);
        Assert.Equal(updateRequest.Priority, result.Priority);
    }

    [Fact]
    public async Task UpdateAsync_ToCompleted_SetsCompletedAt()
    {
        // Arrange
        var taskItem = _builder.CreateTaskItemEntity();
        MockDbContext.SeedTaskItem(_dbContext, taskItem);

        var updateRequest = new UpdateTaskItemRequestV1
        {
            Title = taskItem.Title,
            Status = TaskItemStatus.Completed,
            Priority = (TaskItemPriority)taskItem.Priority,
            CompletionNotes = "Done!",
            SortOrder = taskItem.SortOrder
        };

        // Act
        var result = await _service.UpdateAsync(taskItem.Id, updateRequest);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TaskItemStatus.Completed, result.Status);
        Assert.NotNull(result.CompletedAt);
        Assert.Equal("Done!", result.CompletionNotes);
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistentTaskItem_ReturnsNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var updateRequest = new UpdateTaskItemRequestV1
        {
            Title = "test",
            Status = TaskItemStatus.InProgress,
            Priority = TaskItemPriority.Medium,
            SortOrder = 0
        };

        // Act
        var result = await _service.UpdateAsync(nonExistentId, updateRequest);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_ToCompletedWithoutNotes_ThrowsInvalidOperationException()
    {
        // Arrange
        var taskItem = _builder.CreateTaskItemEntity();
        MockDbContext.SeedTaskItem(_dbContext, taskItem);

        var updateRequest = new UpdateTaskItemRequestV1
        {
            Title = taskItem.Title,
            Status = TaskItemStatus.Completed,
            Priority = (TaskItemPriority)taskItem.Priority,
            SortOrder = taskItem.SortOrder,
            CompletionNotes = null
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateAsync(taskItem.Id, updateRequest));
    }

    [Fact]
    public async Task UpdateAsync_ToCancelledWithoutNotes_ThrowsInvalidOperationException()
    {
        // Arrange
        var taskItem = _builder.CreateTaskItemEntity();
        MockDbContext.SeedTaskItem(_dbContext, taskItem);

        var updateRequest = new UpdateTaskItemRequestV1
        {
            Title = taskItem.Title,
            Status = TaskItemStatus.Cancelled,
            Priority = (TaskItemPriority)taskItem.Priority,
            SortOrder = taskItem.SortOrder,
            CompletionNotes = null
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateAsync(taskItem.Id, updateRequest));
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithExistingTaskItem_DeletesTaskItem()
    {
        // Arrange
        var taskItem = _builder.CreateTaskItemEntity();
        MockDbContext.SeedTaskItem(_dbContext, taskItem);

        // Act
        var result = await _service.DeleteAsync(taskItem.Id);

        // Assert
        Assert.True(result);

        // Verify deleted from database
        var deletedTaskItem = await _dbContext.TaskItems.FindAsync(taskItem.Id);
        Assert.Null(deletedTaskItem);
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentTaskItem_ReturnsFalse()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _service.DeleteAsync(nonExistentId);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Priority and Status Tests

    [Fact]
    public async Task CreateAsync_DefaultPriorityIsMedium()
    {
        // Arrange
        var request = new CreateTaskItemRequestV1
        {
            Title = "test-task-item"
        };

        // Act
        var result = await _service.CreateAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TaskItemPriority.Medium, result.Priority);
    }

    [Fact]
    public async Task CreateAsync_DefaultStatusIsPending()
    {
        // Arrange
        var request = TestDataBuilder.CreateTaskItemRequest();

        // Act
        var result = await _service.CreateAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TaskItemStatus.Pending, result.Status);
    }

    #endregion

    #region Content Truncation and Chunking Tests

    [Fact]
    public async Task ListAsync_WithLongDescription_IncludesTruncatedPreview()
    {
        // Arrange
        var longDescription = new string('a', 1000);
        var taskItem = _builder.CreateTaskItemEntity();
        taskItem.Description = longDescription;
        _dbContext.TaskItems.Add(taskItem);
        await _dbContext.SaveChangesAsync();

        // Act
        var results = await _service.ListAsync();

        // Assert
        Assert.Single(results);
        var summary = results[0];
        Assert.NotNull(summary.DescriptionPreview);
        Assert.Equal(503, summary.DescriptionPreview.Length); // 500 + "..."
        Assert.EndsWith("...", summary.DescriptionPreview);
        Assert.Equal(1000, summary.DescriptionLength);
    }

    [Fact]
    public async Task ListAsync_WithShortDescription_PreviewEqualsDescription()
    {
        // Arrange
        var shortDescription = "Short description";
        var taskItem = _builder.CreateTaskItemEntity();
        taskItem.Description = shortDescription;
        _dbContext.TaskItems.Add(taskItem);
        await _dbContext.SaveChangesAsync();

        // Act
        var results = await _service.ListAsync();

        // Assert
        Assert.Single(results);
        var summary = results[0];
        Assert.Equal(shortDescription, summary.DescriptionPreview);
        Assert.Equal(shortDescription.Length, summary.DescriptionLength);
    }

    [Fact]
    public async Task ListAsync_WithNullDescription_PreviewIsNull()
    {
        // Arrange
        var taskItem = _builder.CreateTaskItemEntity();
        taskItem.Description = null;
        _dbContext.TaskItems.Add(taskItem);
        await _dbContext.SaveChangesAsync();

        // Act
        var results = await _service.ListAsync();

        // Assert
        Assert.Single(results);
        var summary = results[0];
        Assert.Null(summary.DescriptionPreview);
        Assert.Equal(0, summary.DescriptionLength);
    }

    [Fact]
    public async Task GetByIdAsync_WithoutChunking_ReturnsFullDescription()
    {
        // Arrange
        var longDescription = new string('x', 2000);
        var taskItem = _builder.CreateTaskItemEntity();
        taskItem.Description = longDescription;
        MockDbContext.SeedTaskItem(_dbContext, taskItem);

        // Act
        var result = await _service.GetByIdAsync(taskItem.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(longDescription, result.Description);
        Assert.Equal(2000, result.DescriptionLength);
    }

    [Fact]
    public async Task GetByIdAsync_WithChunking_ReturnsChunkedDescription()
    {
        // Arrange
        var description = "Hello World! This is test content.";
        var taskItem = _builder.CreateTaskItemEntity();
        taskItem.Description = description;
        MockDbContext.SeedTaskItem(_dbContext, taskItem);

        // Act
        var result = await _service.GetByIdAsync(taskItem.Id, contentOffset: 6, contentLength: 5);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("World", result.Description);
        Assert.Equal(description.Length, result.DescriptionLength);
    }

    [Fact]
    public async Task GetByIdAsync_WithOffsetOnly_ReturnsFromOffset()
    {
        // Arrange
        var description = "Hello World!";
        var taskItem = _builder.CreateTaskItemEntity();
        taskItem.Description = description;
        MockDbContext.SeedTaskItem(_dbContext, taskItem);

        // Act
        var result = await _service.GetByIdAsync(taskItem.Id, contentOffset: 6);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("World!", result.Description);
        Assert.Equal(12, result.DescriptionLength);
    }

    #endregion
}
