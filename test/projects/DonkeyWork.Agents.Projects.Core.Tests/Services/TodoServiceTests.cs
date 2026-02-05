using DonkeyWork.Agents.Identity.Contracts.Services;
using DonkeyWork.Agents.Notifications.Contracts.Interfaces;
using DonkeyWork.Agents.Persistence;
using DonkeyWork.Agents.Projects.Contracts.Models;
using DonkeyWork.Agents.Projects.Core.Services;
using DonkeyWork.Agents.Projects.Core.Tests.Helpers;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Projects.Core.Tests.Services;

/// <summary>
/// Unit tests for TodoService.
/// Tests CRUD operations and business logic without external dependencies.
/// </summary>
public class TodoServiceTests : IDisposable
{
    private readonly AgentsDbContext _dbContext;
    private readonly IIdentityContext _identityContext;
    private readonly TodoService _service;
    private readonly Guid _testUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly TestDataBuilder _builder = new();

    public TodoServiceTests()
    {
        (_dbContext, _identityContext) = MockDbContext.CreateWithIdentityContext();
        var notificationService = new Mock<INotificationService>();
        var logger = new Mock<ILogger<TodoService>>();
        _service = new TodoService(_dbContext, _identityContext, notificationService.Object, logger.Object);
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
    }

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidRequest_CreatesTodo()
    {
        // Arrange
        var request = new CreateTodoRequestV1
        {
            Title = "test-todo",
            Description = "Test description",
            Priority = TodoPriority.High
        };

        // Act
        var result = await _service.CreateAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(request.Title, result.Title);
        Assert.Equal(request.Description, result.Description);
        Assert.Equal(request.Priority, result.Priority);
        Assert.Equal(TodoStatus.Pending, result.Status);

        // Verify todo was created in database
        var todoInDb = await _dbContext.Todos.FindAsync(result.Id);
        Assert.NotNull(todoInDb);
        Assert.Equal(_testUserId, todoInDb.UserId);
    }

    [Fact]
    public async Task CreateAsync_StandaloneTodo_HasNullProjectAndMilestone()
    {
        // Arrange
        var request = TestDataBuilder.CreateTodoRequest();

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

        var request = new CreateTodoRequestV1
        {
            Title = "test-todo",
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

        var request = new CreateTodoRequestV1
        {
            Title = "test-todo",
            MilestoneId = milestone.Id
        };

        // Act
        var result = await _service.CreateAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(milestone.Id, result.MilestoneId);
    }

    [Fact]
    public async Task CreateAsync_WithTags_CreatesTodoWithTags()
    {
        // Arrange
        var request = new CreateTodoRequestV1
        {
            Title = "test-todo",
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
        foreach (var priority in Enum.GetValues<TodoPriority>())
        {
            var request = new CreateTodoRequestV1
            {
                Title = $"todo-{priority}",
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
    public async Task GetByIdAsync_WithExistingTodo_ReturnsTodo()
    {
        // Arrange
        var todo = _builder.CreateTodoEntity();
        MockDbContext.SeedTodo(_dbContext, todo);

        // Act
        var result = await _service.GetByIdAsync(todo.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(todo.Id, result.Id);
        Assert.Equal(todo.Title, result.Title);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentTodo_ReturnsNull()
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
    public async Task ListAsync_WithMultipleTodos_ReturnsAllUserTodos()
    {
        // Arrange
        var todo1 = _builder.CreateTodoEntity(title: "todo-1");
        var todo2 = _builder.CreateTodoEntity(title: "todo-2");
        var todo3 = _builder.CreateTodoEntity(title: "todo-3");
        _dbContext.Todos.AddRange(todo1, todo2, todo3);
        await _dbContext.SaveChangesAsync();

        // Act
        var results = await _service.ListAsync();

        // Assert
        Assert.NotNull(results);
        Assert.Equal(3, results.Count);
    }

    [Fact]
    public async Task ListAsync_WithNoTodos_ReturnsEmptyList()
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
    public async Task GetStandaloneAsync_ReturnsOnlyStandaloneTodos()
    {
        // Arrange
        var project = _builder.CreateProjectEntity();
        MockDbContext.SeedProject(_dbContext, project);

        var standaloneTodo = _builder.CreateTodoEntity(title: "standalone");
        var projectTodo = _builder.CreateTodoEntity(title: "project-todo", projectId: project.Id);

        _dbContext.Todos.AddRange(standaloneTodo, projectTodo);
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
    public async Task UpdateAsync_WithValidRequest_UpdatesTodo()
    {
        // Arrange
        var todo = _builder.CreateTodoEntity();
        MockDbContext.SeedTodo(_dbContext, todo);

        var updateRequest = new UpdateTodoRequestV1
        {
            Title = "updated-todo",
            Description = "Updated description",
            Status = TodoStatus.InProgress,
            Priority = TodoPriority.Critical,
            SortOrder = 10
        };

        // Act
        var result = await _service.UpdateAsync(todo.Id, updateRequest);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(todo.Id, result.Id);
        Assert.Equal(updateRequest.Title, result.Title);
        Assert.Equal(updateRequest.Description, result.Description);
        Assert.Equal(updateRequest.Status, result.Status);
        Assert.Equal(updateRequest.Priority, result.Priority);
    }

    [Fact]
    public async Task UpdateAsync_ToCompleted_SetsCompletedAt()
    {
        // Arrange
        var todo = _builder.CreateTodoEntity();
        MockDbContext.SeedTodo(_dbContext, todo);

        var updateRequest = new UpdateTodoRequestV1
        {
            Title = todo.Title,
            Status = TodoStatus.Completed,
            Priority = (TodoPriority)todo.Priority,
            CompletionNotes = "Done!",
            SortOrder = todo.SortOrder
        };

        // Act
        var result = await _service.UpdateAsync(todo.Id, updateRequest);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TodoStatus.Completed, result.Status);
        Assert.NotNull(result.CompletedAt);
        Assert.Equal("Done!", result.CompletionNotes);
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistentTodo_ReturnsNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var updateRequest = new UpdateTodoRequestV1
        {
            Title = "test",
            Status = TodoStatus.InProgress,
            Priority = TodoPriority.Medium,
            SortOrder = 0
        };

        // Act
        var result = await _service.UpdateAsync(nonExistentId, updateRequest);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithExistingTodo_DeletesTodo()
    {
        // Arrange
        var todo = _builder.CreateTodoEntity();
        MockDbContext.SeedTodo(_dbContext, todo);

        // Act
        var result = await _service.DeleteAsync(todo.Id);

        // Assert
        Assert.True(result);

        // Verify deleted from database
        var deletedTodo = await _dbContext.Todos.FindAsync(todo.Id);
        Assert.Null(deletedTodo);
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentTodo_ReturnsFalse()
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
        var request = new CreateTodoRequestV1
        {
            Title = "test-todo"
        };

        // Act
        var result = await _service.CreateAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TodoPriority.Medium, result.Priority);
    }

    [Fact]
    public async Task CreateAsync_DefaultStatusIsPending()
    {
        // Arrange
        var request = TestDataBuilder.CreateTodoRequest();

        // Act
        var result = await _service.CreateAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TodoStatus.Pending, result.Status);
    }

    #endregion
}
