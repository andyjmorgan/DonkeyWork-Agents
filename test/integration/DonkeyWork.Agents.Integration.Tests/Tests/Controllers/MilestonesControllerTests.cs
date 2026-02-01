using System.Net;
using DonkeyWork.Agents.Integration.Tests.Base;
using DonkeyWork.Agents.Integration.Tests.Helpers;
using DonkeyWork.Agents.Integration.Tests.Infrastructure.Authentication;
using DonkeyWork.Agents.Integration.Tests.Infrastructure.Containers;
using DonkeyWork.Agents.Projects.Contracts.Models;

namespace DonkeyWork.Agents.Integration.Tests.Tests.Controllers;

[Trait("Category", "Integration")]
public class MilestonesControllerTests : ControllerIntegrationTestBase
{
    private const string ProjectsBaseUrl = "/api/v1/projects";

    public MilestonesControllerTests(InfrastructureFixture infrastructure)
        : base(infrastructure)
    {
    }

    private string MilestonesUrl(Guid projectId) => $"{ProjectsBaseUrl}/{projectId}/milestones";

    private async Task<ProjectDetailsV1> CreateProjectAsync()
    {
        return (await PostAsync<ProjectDetailsV1>(ProjectsBaseUrl, TestDataBuilder.CreateProjectRequest()))!;
    }

    #region Create Tests

    [Fact]
    public async Task Create_WithValidRequest_ReturnsCreatedMilestone()
    {
        // Arrange
        var project = await CreateProjectAsync();
        var request = TestDataBuilder.CreateMilestoneRequest(
            name: "Sprint 1",
            description: "First sprint milestone");

        // Act
        var response = await PostResponseAsync(MilestonesUrl(project.Id), request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var milestone = await response.Content.ReadFromJsonAsync<MilestoneDetailsV1>(JsonOptions);
        Assert.NotNull(milestone);
        Assert.NotEqual(Guid.Empty, milestone.Id);
        Assert.Equal(project.Id, milestone.ProjectId);
        Assert.Equal("Sprint 1", milestone.Name);
        Assert.Equal("First sprint milestone", milestone.Description);
        Assert.Equal(MilestoneStatus.NotStarted, milestone.Status);
    }

    [Fact]
    public async Task Create_WithNonExistentProject_ReturnsNotFound()
    {
        // Arrange
        var nonExistentProjectId = Guid.NewGuid();
        var request = TestDataBuilder.CreateMilestoneRequest();

        // Act
        var response = await PostResponseAsync(MilestonesUrl(nonExistentProjectId), request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region Get Tests

    [Fact]
    public async Task Get_ExistingMilestone_ReturnsMilestone()
    {
        // Arrange
        var project = await CreateProjectAsync();
        var created = await PostAsync<MilestoneDetailsV1>(
            MilestonesUrl(project.Id),
            TestDataBuilder.CreateMilestoneRequest());

        // Act
        var milestone = await GetAsync<MilestoneDetailsV1>($"{MilestonesUrl(project.Id)}/{created!.Id}");

        // Assert
        Assert.NotNull(milestone);
        Assert.Equal(created.Id, milestone.Id);
        Assert.Equal(created.Name, milestone.Name);
    }

    [Fact]
    public async Task Get_NonExistingMilestone_ReturnsNotFound()
    {
        // Arrange
        var project = await CreateProjectAsync();

        // Act
        var response = await GetResponseAsync($"{MilestonesUrl(project.Id)}/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region List Tests

    [Fact]
    public async Task List_ReturnsAllProjectMilestones()
    {
        // Arrange
        var project = await CreateProjectAsync();
        await PostAsync<MilestoneDetailsV1>(MilestonesUrl(project.Id), TestDataBuilder.CreateMilestoneRequest("Sprint 1"));
        await PostAsync<MilestoneDetailsV1>(MilestonesUrl(project.Id), TestDataBuilder.CreateMilestoneRequest("Sprint 2"));
        await PostAsync<MilestoneDetailsV1>(MilestonesUrl(project.Id), TestDataBuilder.CreateMilestoneRequest("Sprint 3"));

        // Act
        var milestones = await GetAsync<List<MilestoneSummaryV1>>(MilestonesUrl(project.Id));

        // Assert
        Assert.NotNull(milestones);
        Assert.Equal(3, milestones.Count);
    }

    [Fact]
    public async Task List_WithNoMilestones_ReturnsEmptyList()
    {
        // Arrange
        var project = await CreateProjectAsync();

        // Act
        var milestones = await GetAsync<List<MilestoneSummaryV1>>(MilestonesUrl(project.Id));

        // Assert
        Assert.NotNull(milestones);
        Assert.Empty(milestones);
    }

    [Fact]
    public async Task List_OnlyReturnsMilestonesForSpecificProject()
    {
        // Arrange - Create two projects with milestones
        var project1 = await CreateProjectAsync();
        var project2 = await CreateProjectAsync();

        await PostAsync<MilestoneDetailsV1>(MilestonesUrl(project1.Id), TestDataBuilder.CreateMilestoneRequest("Project1 Milestone"));
        await PostAsync<MilestoneDetailsV1>(MilestonesUrl(project2.Id), TestDataBuilder.CreateMilestoneRequest("Project2 Milestone"));

        // Act
        var milestones = await GetAsync<List<MilestoneSummaryV1>>(MilestonesUrl(project1.Id));

        // Assert
        Assert.NotNull(milestones);
        Assert.Single(milestones);
        Assert.Equal("Project1 Milestone", milestones[0].Name);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_ExistingMilestone_ReturnsUpdatedMilestone()
    {
        // Arrange
        var project = await CreateProjectAsync();
        var created = await PostAsync<MilestoneDetailsV1>(
            MilestonesUrl(project.Id),
            TestDataBuilder.CreateMilestoneRequest("Original"));

        var updateRequest = TestDataBuilder.UpdateMilestoneRequest(
            name: "Updated Sprint",
            description: "Updated description",
            status: MilestoneStatus.InProgress);

        // Act
        var updated = await PutAsync<MilestoneDetailsV1>($"{MilestonesUrl(project.Id)}/{created!.Id}", updateRequest);

        // Assert
        Assert.NotNull(updated);
        Assert.Equal(created.Id, updated.Id);
        Assert.Equal("Updated Sprint", updated.Name);
        Assert.Equal("Updated description", updated.Description);
        Assert.Equal(MilestoneStatus.InProgress, updated.Status);
    }

    [Fact]
    public async Task Update_NonExistingMilestone_ReturnsNotFound()
    {
        // Arrange
        var project = await CreateProjectAsync();
        var updateRequest = TestDataBuilder.UpdateMilestoneRequest();

        // Act
        var response = await PutResponseAsync($"{MilestonesUrl(project.Id)}/{Guid.NewGuid()}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_CanTransitionThroughAllStatuses()
    {
        // Arrange
        var project = await CreateProjectAsync();
        var created = await PostAsync<MilestoneDetailsV1>(
            MilestonesUrl(project.Id),
            TestDataBuilder.CreateMilestoneRequest());

        // Act & Assert - Transition through all statuses
        var statuses = new[]
        {
            MilestoneStatus.InProgress,
            MilestoneStatus.OnHold,
            MilestoneStatus.Completed,
            MilestoneStatus.NotStarted
        };

        foreach (var status in statuses)
        {
            var updateRequest = TestDataBuilder.UpdateMilestoneRequest(status: status);
            var updated = await PutAsync<MilestoneDetailsV1>($"{MilestonesUrl(project.Id)}/{created!.Id}", updateRequest);
            Assert.Equal(status, updated!.Status);
        }
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_ExistingMilestone_ReturnsNoContent()
    {
        // Arrange
        var project = await CreateProjectAsync();
        var created = await PostAsync<MilestoneDetailsV1>(
            MilestonesUrl(project.Id),
            TestDataBuilder.CreateMilestoneRequest());

        // Act
        var response = await DeleteAsync($"{MilestonesUrl(project.Id)}/{created!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify deletion
        var getResponse = await GetResponseAsync($"{MilestonesUrl(project.Id)}/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_NonExistingMilestone_ReturnsNotFound()
    {
        // Arrange
        var project = await CreateProjectAsync();

        // Act
        var response = await DeleteAsync($"{MilestonesUrl(project.Id)}/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region User Isolation Tests

    [Fact]
    public async Task Get_MilestoneBelongingToAnotherUser_ReturnsNotFound()
    {
        // Arrange - Create project and milestone as user 1
        var user1 = TestUser.CreateRandom();
        SetTestUser(user1);
        var project = await CreateProjectAsync();
        var created = await PostAsync<MilestoneDetailsV1>(
            MilestonesUrl(project.Id),
            TestDataBuilder.CreateMilestoneRequest());

        // Act - Try to get as user 2
        var user2 = TestUser.CreateRandom();
        SetTestUser(user2);
        var response = await GetResponseAsync($"{MilestonesUrl(project.Id)}/{created!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_MilestoneBelongingToAnotherUser_ReturnsNotFound()
    {
        // Arrange - Create project and milestone as user 1
        var user1 = TestUser.CreateRandom();
        SetTestUser(user1);
        var project = await CreateProjectAsync();
        var created = await PostAsync<MilestoneDetailsV1>(
            MilestonesUrl(project.Id),
            TestDataBuilder.CreateMilestoneRequest());

        // Act - Try to delete as user 2
        var user2 = TestUser.CreateRandom();
        SetTestUser(user2);
        var response = await DeleteAsync($"{MilestonesUrl(project.Id)}/{created!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        // Verify milestone still exists for user 1
        SetTestUser(user1);
        var getResponse = await GetResponseAsync($"{MilestonesUrl(project.Id)}/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
    }

    #endregion
}
