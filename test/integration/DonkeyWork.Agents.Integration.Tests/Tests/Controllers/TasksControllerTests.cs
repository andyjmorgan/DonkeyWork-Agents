using System.Net;
using DonkeyWork.Agents.Integration.Tests.Base;
using DonkeyWork.Agents.Integration.Tests.Helpers;
using DonkeyWork.Agents.Integration.Tests.Infrastructure.Authentication;
using DonkeyWork.Agents.Integration.Tests.Infrastructure.Containers;
using DonkeyWork.Agents.Projects.Contracts.Models;

namespace DonkeyWork.Agents.Integration.Tests.Tests.Controllers;

[Trait("Category", "Integration")]
public class TasksControllerTests : ControllerIntegrationTestBase
{
    private const string BaseUrl = "/api/v1/tasks";
    private const string ProjectsBaseUrl = "/api/v1/projects";

    public TasksControllerTests(InfrastructureFixture infrastructure)
        : base(infrastructure)
    {
    }

    private async Task<ProjectDetailsV1> CreateProjectAsync()
    {
        return (await PostAsync<ProjectDetailsV1>(ProjectsBaseUrl, TestDataBuilder.CreateProjectRequest()))!;
    }

    #region Create Tests

    [Fact]
    public async Task Create_StandaloneTaskItem_ReturnsCreatedTaskItem()
    {
        // Arrange
        var request = TestDataBuilder.CreateTaskItemRequest(
            title: "Buy groceries",
            description: "Milk, bread, eggs",
            priority: TaskItemPriority.High);

        // Act
        var response = await PostResponseAsync(BaseUrl, request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var taskItem = await response.Content.ReadFromJsonAsync<TaskItemV1>(JsonOptions);
        Assert.NotNull(taskItem);
        Assert.NotEqual(Guid.Empty, taskItem.Id);
        Assert.Equal("Buy groceries", taskItem.Title);
        Assert.Equal("Milk, bread, eggs", taskItem.Description);
        Assert.Equal(TaskItemPriority.High, taskItem.Priority);
        Assert.Equal(TaskItemStatus.Pending, taskItem.Status);
        Assert.Null(taskItem.ProjectId);
        Assert.Null(taskItem.MilestoneId);
    }

    [Fact]
    public async Task Create_TaskItemWithProject_ReturnsCreatedTaskItem()
    {
        // Arrange
        var project = await CreateProjectAsync();
        var request = TestDataBuilder.CreateTaskItemRequest(
            title: "Project task",
            projectId: project.Id);

        // Act
        var taskItem = await PostAsync<TaskItemV1>(BaseUrl, request);

        // Assert
        Assert.NotNull(taskItem);
        Assert.Equal(project.Id, taskItem.ProjectId);
    }

    [Fact]
    public async Task Create_WithAllPriorities_CreatesSuccessfully()
    {
        // Arrange & Act & Assert
        foreach (var priority in Enum.GetValues<TaskItemPriority>())
        {
            var request = TestDataBuilder.CreateTaskItemRequest(priority: priority);
            var taskItem = await PostAsync<TaskItemV1>(BaseUrl, request);
            Assert.NotNull(taskItem);
            Assert.Equal(priority, taskItem.Priority);
        }
    }

    #endregion

    #region Get Tests

    [Fact]
    public async Task Get_ExistingTaskItem_ReturnsTaskItem()
    {
        // Arrange
        var created = await PostAsync<TaskItemV1>(BaseUrl, TestDataBuilder.CreateTaskItemRequest());

        // Act
        var taskItem = await GetAsync<TaskItemV1>($"{BaseUrl}/{created!.Id}");

        // Assert
        Assert.NotNull(taskItem);
        Assert.Equal(created.Id, taskItem.Id);
        Assert.Equal(created.Title, taskItem.Title);
    }

    [Fact]
    public async Task Get_NonExistingTaskItem_ReturnsNotFound()
    {
        // Act
        var response = await GetResponseAsync($"{BaseUrl}/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region List Tests

    [Fact]
    public async Task List_ReturnsAllUserTaskItems()
    {
        // Arrange
        await PostAsync<TaskItemV1>(BaseUrl, TestDataBuilder.CreateTaskItemRequest("Task 1"));
        await PostAsync<TaskItemV1>(BaseUrl, TestDataBuilder.CreateTaskItemRequest("Task 2"));
        await PostAsync<TaskItemV1>(BaseUrl, TestDataBuilder.CreateTaskItemRequest("Task 3"));

        // Act
        var taskItems = await GetAsync<List<TaskItemV1>>(BaseUrl);

        // Assert
        Assert.NotNull(taskItems);
        Assert.Equal(3, taskItems.Count);
    }

    [Fact]
    public async Task List_WithNoTaskItems_ReturnsEmptyList()
    {
        // Act
        var taskItems = await GetAsync<List<TaskItemV1>>(BaseUrl);

        // Assert
        Assert.NotNull(taskItems);
        Assert.Empty(taskItems);
    }

    [Fact]
    public async Task ListStandalone_ReturnsOnlyStandaloneTaskItems()
    {
        // Arrange
        var project = await CreateProjectAsync();
        await PostAsync<TaskItemV1>(BaseUrl, TestDataBuilder.CreateTaskItemRequest("Standalone 1"));
        await PostAsync<TaskItemV1>(BaseUrl, TestDataBuilder.CreateTaskItemRequest("Standalone 2"));
        await PostAsync<TaskItemV1>(BaseUrl, TestDataBuilder.CreateTaskItemRequest("Project Task", projectId: project.Id));

        // Act
        var taskItems = await GetAsync<List<TaskItemV1>>($"{BaseUrl}/standalone");

        // Assert
        Assert.NotNull(taskItems);
        Assert.Equal(2, taskItems.Count);
        Assert.All(taskItems, t => Assert.Null(t.ProjectId));
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_ExistingTaskItem_ReturnsUpdatedTaskItem()
    {
        // Arrange
        var created = await PostAsync<TaskItemV1>(BaseUrl, TestDataBuilder.CreateTaskItemRequest("Original"));

        var updateRequest = TestDataBuilder.UpdateTaskItemRequest(
            title: "Updated Task",
            description: "Updated description",
            status: TaskItemStatus.InProgress,
            priority: TaskItemPriority.Critical);

        // Act
        var updated = await PutAsync<TaskItemV1>($"{BaseUrl}/{created!.Id}", updateRequest);

        // Assert
        Assert.NotNull(updated);
        Assert.Equal(created.Id, updated.Id);
        Assert.Equal("Updated Task", updated.Title);
        Assert.Equal("Updated description", updated.Description);
        Assert.Equal(TaskItemStatus.InProgress, updated.Status);
        Assert.Equal(TaskItemPriority.Critical, updated.Priority);
    }

    [Fact]
    public async Task Update_NonExistingTaskItem_ReturnsNotFound()
    {
        // Arrange
        var updateRequest = TestDataBuilder.UpdateTaskItemRequest();

        // Act
        var response = await PutResponseAsync($"{BaseUrl}/{Guid.NewGuid()}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_CanTransitionThroughAllStatuses()
    {
        // Arrange
        var created = await PostAsync<TaskItemV1>(BaseUrl, TestDataBuilder.CreateTaskItemRequest());

        // Act & Assert - Transition through all statuses
        var statuses = new[]
        {
            TaskItemStatus.InProgress,
            TaskItemStatus.Completed,
            TaskItemStatus.Cancelled,
            TaskItemStatus.Pending
        };

        foreach (var status in statuses)
        {
            var updateRequest = TestDataBuilder.UpdateTaskItemRequest(status: status);
            var updated = await PutAsync<TaskItemV1>($"{BaseUrl}/{created!.Id}", updateRequest);
            Assert.Equal(status, updated!.Status);
        }
    }

    [Fact]
    public async Task Update_ToCompleted_SetsCompletedAt()
    {
        // Arrange
        var created = await PostAsync<TaskItemV1>(BaseUrl, TestDataBuilder.CreateTaskItemRequest());

        var updateRequest = TestDataBuilder.UpdateTaskItemRequest(status: TaskItemStatus.Completed);

        // Act
        var beforeComplete = DateTimeOffset.UtcNow;
        var updated = await PutAsync<TaskItemV1>($"{BaseUrl}/{created!.Id}", updateRequest);
        var afterComplete = DateTimeOffset.UtcNow;

        // Assert
        Assert.NotNull(updated);
        Assert.Equal(TaskItemStatus.Completed, updated.Status);
        Assert.NotNull(updated.CompletedAt);
        Assert.True(updated.CompletedAt >= beforeComplete.AddSeconds(-1));
        Assert.True(updated.CompletedAt <= afterComplete.AddSeconds(1));
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_ExistingTaskItem_ReturnsNoContent()
    {
        // Arrange
        var created = await PostAsync<TaskItemV1>(BaseUrl, TestDataBuilder.CreateTaskItemRequest());

        // Act
        var response = await DeleteAsync($"{BaseUrl}/{created!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify deletion
        var getResponse = await GetResponseAsync($"{BaseUrl}/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_NonExistingTaskItem_ReturnsNotFound()
    {
        // Act
        var response = await DeleteAsync($"{BaseUrl}/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region User Isolation Tests

    [Fact]
    public async Task Get_TaskItemBelongingToAnotherUser_ReturnsNotFound()
    {
        // Arrange - Create task as user 1
        var user1 = TestUser.CreateRandom();
        SetTestUser(user1);
        var created = await PostAsync<TaskItemV1>(BaseUrl, TestDataBuilder.CreateTaskItemRequest());

        // Act - Try to get as user 2
        var user2 = TestUser.CreateRandom();
        SetTestUser(user2);
        var response = await GetResponseAsync($"{BaseUrl}/{created!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task List_OnlyReturnsTaskItemsForCurrentUser()
    {
        // Arrange - Create tasks for user 1
        var user1 = TestUser.CreateRandom();
        SetTestUser(user1);
        await PostAsync<TaskItemV1>(BaseUrl, TestDataBuilder.CreateTaskItemRequest("User1 Task"));

        // Create tasks for user 2
        var user2 = TestUser.CreateRandom();
        SetTestUser(user2);
        await PostAsync<TaskItemV1>(BaseUrl, TestDataBuilder.CreateTaskItemRequest("User2 Task"));

        // Act - List as user 2
        var taskItems = await GetAsync<List<TaskItemV1>>(BaseUrl);

        // Assert
        Assert.NotNull(taskItems);
        Assert.Single(taskItems);
        Assert.Equal("User2 Task", taskItems[0].Title);
    }

    [Fact]
    public async Task Delete_TaskItemBelongingToAnotherUser_ReturnsNotFound()
    {
        // Arrange - Create task as user 1
        var user1 = TestUser.CreateRandom();
        SetTestUser(user1);
        var created = await PostAsync<TaskItemV1>(BaseUrl, TestDataBuilder.CreateTaskItemRequest());

        // Act - Try to delete as user 2
        var user2 = TestUser.CreateRandom();
        SetTestUser(user2);
        var response = await DeleteAsync($"{BaseUrl}/{created!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        // Verify task still exists for user 1
        SetTestUser(user1);
        var getResponse = await GetResponseAsync($"{BaseUrl}/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
    }

    #endregion
}
