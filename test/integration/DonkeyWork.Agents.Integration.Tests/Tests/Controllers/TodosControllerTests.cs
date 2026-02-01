using System.Net;
using DonkeyWork.Agents.Integration.Tests.Base;
using DonkeyWork.Agents.Integration.Tests.Helpers;
using DonkeyWork.Agents.Integration.Tests.Infrastructure.Authentication;
using DonkeyWork.Agents.Integration.Tests.Infrastructure.Containers;
using DonkeyWork.Agents.Projects.Contracts.Models;

namespace DonkeyWork.Agents.Integration.Tests.Tests.Controllers;

[Trait("Category", "Integration")]
public class TodosControllerTests : ControllerIntegrationTestBase
{
    private const string BaseUrl = "/api/v1/todos";
    private const string ProjectsBaseUrl = "/api/v1/projects";

    public TodosControllerTests(InfrastructureFixture infrastructure)
        : base(infrastructure)
    {
    }

    private async Task<ProjectDetailsV1> CreateProjectAsync()
    {
        return (await PostAsync<ProjectDetailsV1>(ProjectsBaseUrl, TestDataBuilder.CreateProjectRequest()))!;
    }

    #region Create Tests

    [Fact]
    public async Task Create_StandaloneTodo_ReturnsCreatedTodo()
    {
        // Arrange
        var request = TestDataBuilder.CreateTodoRequest(
            title: "Buy groceries",
            description: "Milk, bread, eggs",
            priority: TodoPriority.High);

        // Act
        var response = await PostResponseAsync(BaseUrl, request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var todo = await response.Content.ReadFromJsonAsync<TodoV1>(JsonOptions);
        Assert.NotNull(todo);
        Assert.NotEqual(Guid.Empty, todo.Id);
        Assert.Equal("Buy groceries", todo.Title);
        Assert.Equal("Milk, bread, eggs", todo.Description);
        Assert.Equal(TodoPriority.High, todo.Priority);
        Assert.Equal(TodoStatus.Pending, todo.Status);
        Assert.Null(todo.ProjectId);
        Assert.Null(todo.MilestoneId);
    }

    [Fact]
    public async Task Create_TodoWithProject_ReturnsCreatedTodo()
    {
        // Arrange
        var project = await CreateProjectAsync();
        var request = TestDataBuilder.CreateTodoRequest(
            title: "Project task",
            projectId: project.Id);

        // Act
        var todo = await PostAsync<TodoV1>(BaseUrl, request);

        // Assert
        Assert.NotNull(todo);
        Assert.Equal(project.Id, todo.ProjectId);
    }

    [Fact]
    public async Task Create_WithAllPriorities_CreatesSuccessfully()
    {
        // Arrange & Act & Assert
        foreach (var priority in Enum.GetValues<TodoPriority>())
        {
            var request = TestDataBuilder.CreateTodoRequest(priority: priority);
            var todo = await PostAsync<TodoV1>(BaseUrl, request);
            Assert.NotNull(todo);
            Assert.Equal(priority, todo.Priority);
        }
    }

    #endregion

    #region Get Tests

    [Fact]
    public async Task Get_ExistingTodo_ReturnsTodo()
    {
        // Arrange
        var created = await PostAsync<TodoV1>(BaseUrl, TestDataBuilder.CreateTodoRequest());

        // Act
        var todo = await GetAsync<TodoV1>($"{BaseUrl}/{created!.Id}");

        // Assert
        Assert.NotNull(todo);
        Assert.Equal(created.Id, todo.Id);
        Assert.Equal(created.Title, todo.Title);
    }

    [Fact]
    public async Task Get_NonExistingTodo_ReturnsNotFound()
    {
        // Act
        var response = await GetResponseAsync($"{BaseUrl}/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region List Tests

    [Fact]
    public async Task List_ReturnsAllUserTodos()
    {
        // Arrange
        await PostAsync<TodoV1>(BaseUrl, TestDataBuilder.CreateTodoRequest("Todo 1"));
        await PostAsync<TodoV1>(BaseUrl, TestDataBuilder.CreateTodoRequest("Todo 2"));
        await PostAsync<TodoV1>(BaseUrl, TestDataBuilder.CreateTodoRequest("Todo 3"));

        // Act
        var todos = await GetAsync<List<TodoV1>>(BaseUrl);

        // Assert
        Assert.NotNull(todos);
        Assert.Equal(3, todos.Count);
    }

    [Fact]
    public async Task List_WithNoTodos_ReturnsEmptyList()
    {
        // Act
        var todos = await GetAsync<List<TodoV1>>(BaseUrl);

        // Assert
        Assert.NotNull(todos);
        Assert.Empty(todos);
    }

    [Fact]
    public async Task ListStandalone_ReturnsOnlyStandaloneTodos()
    {
        // Arrange
        var project = await CreateProjectAsync();
        await PostAsync<TodoV1>(BaseUrl, TestDataBuilder.CreateTodoRequest("Standalone 1"));
        await PostAsync<TodoV1>(BaseUrl, TestDataBuilder.CreateTodoRequest("Standalone 2"));
        await PostAsync<TodoV1>(BaseUrl, TestDataBuilder.CreateTodoRequest("Project Todo", projectId: project.Id));

        // Act
        var todos = await GetAsync<List<TodoV1>>($"{BaseUrl}/standalone");

        // Assert
        Assert.NotNull(todos);
        Assert.Equal(2, todos.Count);
        Assert.All(todos, t => Assert.Null(t.ProjectId));
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_ExistingTodo_ReturnsUpdatedTodo()
    {
        // Arrange
        var created = await PostAsync<TodoV1>(BaseUrl, TestDataBuilder.CreateTodoRequest("Original"));

        var updateRequest = TestDataBuilder.UpdateTodoRequest(
            title: "Updated Task",
            description: "Updated description",
            status: TodoStatus.InProgress,
            priority: TodoPriority.Critical);

        // Act
        var updated = await PutAsync<TodoV1>($"{BaseUrl}/{created!.Id}", updateRequest);

        // Assert
        Assert.NotNull(updated);
        Assert.Equal(created.Id, updated.Id);
        Assert.Equal("Updated Task", updated.Title);
        Assert.Equal("Updated description", updated.Description);
        Assert.Equal(TodoStatus.InProgress, updated.Status);
        Assert.Equal(TodoPriority.Critical, updated.Priority);
    }

    [Fact]
    public async Task Update_NonExistingTodo_ReturnsNotFound()
    {
        // Arrange
        var updateRequest = TestDataBuilder.UpdateTodoRequest();

        // Act
        var response = await PutResponseAsync($"{BaseUrl}/{Guid.NewGuid()}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_CanTransitionThroughAllStatuses()
    {
        // Arrange
        var created = await PostAsync<TodoV1>(BaseUrl, TestDataBuilder.CreateTodoRequest());

        // Act & Assert - Transition through all statuses
        var statuses = new[]
        {
            TodoStatus.InProgress,
            TodoStatus.Completed,
            TodoStatus.Cancelled,
            TodoStatus.Pending
        };

        foreach (var status in statuses)
        {
            var updateRequest = TestDataBuilder.UpdateTodoRequest(status: status);
            var updated = await PutAsync<TodoV1>($"{BaseUrl}/{created!.Id}", updateRequest);
            Assert.Equal(status, updated!.Status);
        }
    }

    [Fact]
    public async Task Update_ToCompleted_SetsCompletedAt()
    {
        // Arrange
        var created = await PostAsync<TodoV1>(BaseUrl, TestDataBuilder.CreateTodoRequest());

        var updateRequest = TestDataBuilder.UpdateTodoRequest(status: TodoStatus.Completed);

        // Act
        var beforeComplete = DateTimeOffset.UtcNow;
        var updated = await PutAsync<TodoV1>($"{BaseUrl}/{created!.Id}", updateRequest);
        var afterComplete = DateTimeOffset.UtcNow;

        // Assert
        Assert.NotNull(updated);
        Assert.Equal(TodoStatus.Completed, updated.Status);
        Assert.NotNull(updated.CompletedAt);
        Assert.True(updated.CompletedAt >= beforeComplete.AddSeconds(-1));
        Assert.True(updated.CompletedAt <= afterComplete.AddSeconds(1));
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_ExistingTodo_ReturnsNoContent()
    {
        // Arrange
        var created = await PostAsync<TodoV1>(BaseUrl, TestDataBuilder.CreateTodoRequest());

        // Act
        var response = await DeleteAsync($"{BaseUrl}/{created!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify deletion
        var getResponse = await GetResponseAsync($"{BaseUrl}/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_NonExistingTodo_ReturnsNotFound()
    {
        // Act
        var response = await DeleteAsync($"{BaseUrl}/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region User Isolation Tests

    [Fact]
    public async Task Get_TodoBelongingToAnotherUser_ReturnsNotFound()
    {
        // Arrange - Create todo as user 1
        var user1 = TestUser.CreateRandom();
        SetTestUser(user1);
        var created = await PostAsync<TodoV1>(BaseUrl, TestDataBuilder.CreateTodoRequest());

        // Act - Try to get as user 2
        var user2 = TestUser.CreateRandom();
        SetTestUser(user2);
        var response = await GetResponseAsync($"{BaseUrl}/{created!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task List_OnlyReturnsTodosForCurrentUser()
    {
        // Arrange - Create todos for user 1
        var user1 = TestUser.CreateRandom();
        SetTestUser(user1);
        await PostAsync<TodoV1>(BaseUrl, TestDataBuilder.CreateTodoRequest("User1 Todo"));

        // Create todos for user 2
        var user2 = TestUser.CreateRandom();
        SetTestUser(user2);
        await PostAsync<TodoV1>(BaseUrl, TestDataBuilder.CreateTodoRequest("User2 Todo"));

        // Act - List as user 2
        var todos = await GetAsync<List<TodoV1>>(BaseUrl);

        // Assert
        Assert.NotNull(todos);
        Assert.Single(todos);
        Assert.Equal("User2 Todo", todos[0].Title);
    }

    [Fact]
    public async Task Delete_TodoBelongingToAnotherUser_ReturnsNotFound()
    {
        // Arrange - Create todo as user 1
        var user1 = TestUser.CreateRandom();
        SetTestUser(user1);
        var created = await PostAsync<TodoV1>(BaseUrl, TestDataBuilder.CreateTodoRequest());

        // Act - Try to delete as user 2
        var user2 = TestUser.CreateRandom();
        SetTestUser(user2);
        var response = await DeleteAsync($"{BaseUrl}/{created!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        // Verify todo still exists for user 1
        SetTestUser(user1);
        var getResponse = await GetResponseAsync($"{BaseUrl}/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
    }

    #endregion
}
