using System.Net;
using DonkeyWork.Agents.Integration.Tests.Base;
using DonkeyWork.Agents.Integration.Tests.Helpers;
using DonkeyWork.Agents.Integration.Tests.Infrastructure.Authentication;
using DonkeyWork.Agents.Integration.Tests.Infrastructure.Containers;
using DonkeyWork.Agents.Projects.Contracts.Models;

namespace DonkeyWork.Agents.Integration.Tests.Tests.Controllers;

[Trait("Category", "Integration")]
public class ProjectsControllerTests : ControllerIntegrationTestBase
{
    private const string BaseUrl = "/api/v1/projects";

    public ProjectsControllerTests(InfrastructureFixture infrastructure)
        : base(infrastructure)
    {
    }

    #region Create Tests

    [Fact]
    public async Task Create_WithValidRequest_ReturnsCreatedProject()
    {
        // Arrange
        var request = TestDataBuilder.CreateProjectRequest(
            name: "My Test Project",
            content: "A project for testing",
            successCriteria: "All tests pass",
            status: ProjectStatus.NotStarted);

        // Act
        var response = await PostResponseAsync(BaseUrl, request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var project = await response.Content.ReadFromJsonAsync<ProjectDetailsV1>(JsonOptions);
        Assert.NotNull(project);
        Assert.NotEqual(Guid.Empty, project.Id);
        Assert.Equal(request.Name, project.Name);
        Assert.Equal(request.Content, project.Content);
        Assert.Equal(request.SuccessCriteria, project.SuccessCriteria);
        Assert.Equal(request.Status, project.Status);
    }

    [Fact]
    public async Task Create_WithMinimalRequest_ReturnsCreatedProject()
    {
        // Arrange
        var request = new CreateProjectRequestV1
        {
            Name = "Minimal Project"
        };

        // Act
        var response = await PostResponseAsync(BaseUrl, request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var project = await response.Content.ReadFromJsonAsync<ProjectDetailsV1>(JsonOptions);
        Assert.NotNull(project);
        Assert.Equal("Minimal Project", project.Name);
        Assert.Equal(ProjectStatus.NotStarted, project.Status);
    }

    #endregion

    #region Get Tests

    [Fact]
    public async Task Get_ExistingProject_ReturnsProject()
    {
        // Arrange
        var createRequest = TestDataBuilder.CreateProjectRequest();
        var created = await PostAsync<ProjectDetailsV1>(BaseUrl, createRequest);

        // Act
        var project = await GetAsync<ProjectDetailsV1>($"{BaseUrl}/{created!.Id}");

        // Assert
        Assert.NotNull(project);
        Assert.Equal(created.Id, project.Id);
        Assert.Equal(created.Name, project.Name);
    }

    [Fact]
    public async Task Get_NonExistingProject_ReturnsNotFound()
    {
        // Act
        var response = await GetResponseAsync($"{BaseUrl}/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region List Tests

    [Fact]
    public async Task List_ReturnsAllUserProjects()
    {
        // Arrange
        await PostAsync<ProjectDetailsV1>(BaseUrl, TestDataBuilder.CreateProjectRequest("Project One"));
        await PostAsync<ProjectDetailsV1>(BaseUrl, TestDataBuilder.CreateProjectRequest("Project Two"));
        await PostAsync<ProjectDetailsV1>(BaseUrl, TestDataBuilder.CreateProjectRequest("Project Three"));

        // Act
        var projects = await GetAsync<List<ProjectSummaryV1>>(BaseUrl);

        // Assert
        Assert.NotNull(projects);
        Assert.Equal(3, projects.Count);
    }

    [Fact]
    public async Task List_WithNoProjects_ReturnsEmptyList()
    {
        // Act
        var projects = await GetAsync<List<ProjectSummaryV1>>(BaseUrl);

        // Assert
        Assert.NotNull(projects);
        Assert.Empty(projects);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_ExistingProject_ReturnsUpdatedProject()
    {
        // Arrange
        var createRequest = TestDataBuilder.CreateProjectRequest("Original Name");
        var created = await PostAsync<ProjectDetailsV1>(BaseUrl, createRequest);

        var updateRequest = TestDataBuilder.UpdateProjectRequest(
            name: "Updated Name",
            content: "Updated content",
            status: ProjectStatus.InProgress);

        // Act
        var updated = await PutAsync<ProjectDetailsV1>($"{BaseUrl}/{created!.Id}", updateRequest);

        // Assert
        Assert.NotNull(updated);
        Assert.Equal(created.Id, updated.Id);
        Assert.Equal("Updated Name", updated.Name);
        Assert.Equal("Updated content", updated.Content);
        Assert.Equal(ProjectStatus.InProgress, updated.Status);
    }

    [Fact]
    public async Task Update_NonExistingProject_ReturnsNotFound()
    {
        // Arrange
        var updateRequest = TestDataBuilder.UpdateProjectRequest();

        // Act
        var response = await PutResponseAsync($"{BaseUrl}/{Guid.NewGuid()}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_ExistingProject_ReturnsNoContent()
    {
        // Arrange
        var createRequest = TestDataBuilder.CreateProjectRequest();
        var created = await PostAsync<ProjectDetailsV1>(BaseUrl, createRequest);

        // Act
        var response = await DeleteAsync($"{BaseUrl}/{created!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify deletion
        var getResponse = await GetResponseAsync($"{BaseUrl}/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_NonExistingProject_ReturnsNotFound()
    {
        // Act
        var response = await DeleteAsync($"{BaseUrl}/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region User Isolation Tests

    [Fact]
    public async Task Get_ProjectBelongingToAnotherUser_ReturnsNotFound()
    {
        // Arrange - Create project as user 1
        var user1 = TestUser.CreateRandom();
        SetTestUser(user1);
        var created = await PostAsync<ProjectDetailsV1>(BaseUrl, TestDataBuilder.CreateProjectRequest());

        // Act - Try to get as user 2
        var user2 = TestUser.CreateRandom();
        SetTestUser(user2);
        var response = await GetResponseAsync($"{BaseUrl}/{created!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task List_OnlyReturnsProjectsForCurrentUser()
    {
        // Arrange - Create projects for user 1
        var user1 = TestUser.CreateRandom();
        SetTestUser(user1);
        await PostAsync<ProjectDetailsV1>(BaseUrl, TestDataBuilder.CreateProjectRequest("User1 Project"));

        // Create projects for user 2
        var user2 = TestUser.CreateRandom();
        SetTestUser(user2);
        await PostAsync<ProjectDetailsV1>(BaseUrl, TestDataBuilder.CreateProjectRequest("User2 Project"));

        // Act - List as user 2
        var projects = await GetAsync<List<ProjectSummaryV1>>(BaseUrl);

        // Assert
        Assert.NotNull(projects);
        Assert.Single(projects);
        Assert.Equal("User2 Project", projects[0].Name);
    }

    [Fact]
    public async Task Update_ProjectBelongingToAnotherUser_ReturnsNotFound()
    {
        // Arrange - Create project as user 1
        var user1 = TestUser.CreateRandom();
        SetTestUser(user1);
        var created = await PostAsync<ProjectDetailsV1>(BaseUrl, TestDataBuilder.CreateProjectRequest());

        // Act - Try to update as user 2
        var user2 = TestUser.CreateRandom();
        SetTestUser(user2);
        var response = await PutResponseAsync($"{BaseUrl}/{created!.Id}", TestDataBuilder.UpdateProjectRequest("Hacked"));

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ProjectBelongingToAnotherUser_ReturnsNotFound()
    {
        // Arrange - Create project as user 1
        var user1 = TestUser.CreateRandom();
        SetTestUser(user1);
        var created = await PostAsync<ProjectDetailsV1>(BaseUrl, TestDataBuilder.CreateProjectRequest());

        // Act - Try to delete as user 2
        var user2 = TestUser.CreateRandom();
        SetTestUser(user2);
        var response = await DeleteAsync($"{BaseUrl}/{created!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        // Verify project still exists for user 1
        SetTestUser(user1);
        var getResponse = await GetResponseAsync($"{BaseUrl}/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
    }

    #endregion

    #region Status Workflow Tests

    [Fact]
    public async Task Update_CanTransitionThroughAllStatuses()
    {
        // Arrange
        var created = await PostAsync<ProjectDetailsV1>(BaseUrl, TestDataBuilder.CreateProjectRequest());

        // Act & Assert - Transition through all statuses
        var statuses = new[]
        {
            ProjectStatus.InProgress,
            ProjectStatus.OnHold,
            ProjectStatus.Completed,
            ProjectStatus.NotStarted
        };

        foreach (var status in statuses)
        {
            var updateRequest = TestDataBuilder.UpdateProjectRequest(status: status);
            var updated = await PutAsync<ProjectDetailsV1>($"{BaseUrl}/{created!.Id}", updateRequest);
            Assert.Equal(status, updated!.Status);
        }
    }

    #endregion
}
