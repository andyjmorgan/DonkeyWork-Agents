using DonkeyWork.Agents.Identity.Contracts.Services;
using DonkeyWork.Agents.Persistence;
using DonkeyWork.Agents.Projects.Contracts.Models;
using DonkeyWork.Agents.Projects.Core.Services;
using DonkeyWork.Agents.Projects.Core.Tests.Helpers;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Projects.Core.Tests.Services;

/// <summary>
/// Unit tests for ProjectService.
/// Tests CRUD operations and business logic without external dependencies.
/// </summary>
public class ProjectServiceTests : IDisposable
{
    private readonly AgentsDbContext _dbContext;
    private readonly IIdentityContext _identityContext;
    private readonly ProjectService _service;
    private readonly Guid _testUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly TestDataBuilder _builder = new();

    public ProjectServiceTests()
    {
        (_dbContext, _identityContext) = MockDbContext.CreateWithIdentityContext();
        var logger = new Mock<ILogger<ProjectService>>();
        _service = new ProjectService(_dbContext, _identityContext, logger.Object);
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
    }

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidRequest_CreatesProject()
    {
        // Arrange
        var request = new CreateProjectRequestV1
        {
            Name = "test-project",
            Content = "Test content"
        };

        // Act
        var result = await _service.CreateAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(request.Name, result.Name);
        Assert.Equal(request.Content, result.Content);
        Assert.Equal(ProjectStatus.NotStarted, result.Status);
        Assert.True(result.CreatedAt <= DateTimeOffset.UtcNow);

        // Verify project was created in database
        var projectInDb = await _dbContext.Projects.FindAsync(result.Id);
        Assert.NotNull(projectInDb);
        Assert.Equal(_testUserId, projectInDb.UserId);
    }

    [Fact]
    public async Task CreateAsync_WithTags_CreatesProjectWithTags()
    {
        // Arrange
        var request = new CreateProjectRequestV1
        {
            Name = "test-project",
            Tags = new List<TagRequestV1>
            {
                new() { Name = "tag1", Color = "#ff0000" },
                new() { Name = "tag2", Color = "#00ff00" }
            }
        };

        // Act
        var result = await _service.CreateAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Tags.Count);
        Assert.Contains(result.Tags, t => t.Name == "tag1");
        Assert.Contains(result.Tags, t => t.Name == "tag2");
    }

    [Fact]
    public async Task CreateAsync_WithNullContent_CreatesSuccessfully()
    {
        // Arrange
        var request = new CreateProjectRequestV1
        {
            Name = "test-project",
            Content = null
        };

        // Act
        var result = await _service.CreateAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Content);
    }

    [Fact]
    public async Task CreateAsync_MultipleProjects_EachHasUniqueIds()
    {
        // Arrange
        var request1 = TestDataBuilder.CreateProjectRequest("project-1");
        var request2 = TestDataBuilder.CreateProjectRequest("project-2");

        // Act
        var result1 = await _service.CreateAsync(request1);
        var result2 = await _service.CreateAsync(request2);

        // Assert
        Assert.NotEqual(result1.Id, result2.Id);
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithExistingProject_ReturnsProject()
    {
        // Arrange
        var project = _builder.CreateProjectEntity();
        MockDbContext.SeedProject(_dbContext, project);

        // Act
        var result = await _service.GetByIdAsync(project.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(project.Id, result.Id);
        Assert.Equal(project.Name, result.Name);
        Assert.Equal(project.Content, result.Content);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentProject_ReturnsNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _service.GetByIdAsync(nonExistentId);

        // Assert
        Assert.Null(result);
    }

    // Note: User isolation tests removed - user filtering is handled by DbContext global query filter
    // using IIdentityContext.UserId, not by the userId parameter passed to service methods.
    // User isolation should be tested at the DbContext/integration test level.

    #endregion

    #region ListAsync Tests

    [Fact]
    public async Task ListAsync_WithMultipleProjects_ReturnsAllUserProjects()
    {
        // Arrange
        var project1 = _builder.CreateProjectEntity(name: "project-1");
        var project2 = _builder.CreateProjectEntity(name: "project-2");
        var project3 = _builder.CreateProjectEntity(name: "project-3");
        _dbContext.Projects.AddRange(project1, project2, project3);
        await _dbContext.SaveChangesAsync();

        // Act
        var results = await _service.ListAsync();

        // Assert
        Assert.NotNull(results);
        Assert.Equal(3, results.Count);
        Assert.Contains(results, p => p.Id == project1.Id);
        Assert.Contains(results, p => p.Id == project2.Id);
        Assert.Contains(results, p => p.Id == project3.Id);
    }

    [Fact]
    public async Task ListAsync_WithNoProjects_ReturnsEmptyList()
    {
        // Act
        var results = await _service.ListAsync();

        // Assert
        Assert.NotNull(results);
        Assert.Empty(results);
    }

    [Fact]
    public async Task ListAsync_OrdersByCreatedAtDescending()
    {
        // Arrange
        var project1 = _builder.CreateProjectEntity(name: "oldest");
        await Task.Delay(10);
        var project2 = _builder.CreateProjectEntity(name: "middle");
        await Task.Delay(10);
        var project3 = _builder.CreateProjectEntity(name: "newest");

        _dbContext.Projects.AddRange(project1, project2, project3);
        await _dbContext.SaveChangesAsync();

        // Act
        var results = await _service.ListAsync();

        // Assert
        Assert.Equal(3, results.Count);
        Assert.Equal(project3.Id, results[0].Id);
        Assert.Equal(project2.Id, results[1].Id);
        Assert.Equal(project1.Id, results[2].Id);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidRequest_UpdatesProject()
    {
        // Arrange
        var project = _builder.CreateProjectEntity();
        MockDbContext.SeedProject(_dbContext, project);

        var updateRequest = new UpdateProjectRequestV1
        {
            Name = "updated-name",
            Content = "Updated content",
            Status = ProjectStatus.InProgress
        };

        // Act
        var result = await _service.UpdateAsync(project.Id, updateRequest);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(project.Id, result.Id);
        Assert.Equal(updateRequest.Name, result.Name);
        Assert.Equal(updateRequest.Content, result.Content);
        Assert.Equal(updateRequest.Status, result.Status);

        // Verify in database
        var updatedProject = await _dbContext.Projects.FindAsync(project.Id);
        Assert.NotNull(updatedProject);
        Assert.Equal(updateRequest.Name, updatedProject.Name);
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistentProject_ReturnsNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var updateRequest = new UpdateProjectRequestV1
        {
            Name = "test",
            Status = ProjectStatus.InProgress
        };

        // Act
        var result = await _service.UpdateAsync(nonExistentId, updateRequest);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesTimestamp()
    {
        // Arrange
        var project = _builder.CreateProjectEntity();
        var originalUpdatedAt = project.UpdatedAt;
        MockDbContext.SeedProject(_dbContext, project);

        await Task.Delay(100);

        var updateRequest = new UpdateProjectRequestV1
        {
            Name = "new-name",
            Status = ProjectStatus.InProgress
        };

        // Act
        var result = await _service.UpdateAsync(project.Id, updateRequest);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.UpdatedAt);
        Assert.True(result.UpdatedAt > originalUpdatedAt);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithExistingProject_DeletesProject()
    {
        // Arrange
        var project = _builder.CreateProjectEntity();
        MockDbContext.SeedProject(_dbContext, project);

        // Act
        var result = await _service.DeleteAsync(project.Id);

        // Assert
        Assert.True(result);

        // Verify deleted from database
        var deletedProject = await _dbContext.Projects.FindAsync(project.Id);
        Assert.Null(deletedProject);
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentProject_ReturnsFalse()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _service.DeleteAsync(nonExistentId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteAsync_CascadesMilestonesDeletion()
    {
        // Arrange
        var project = _builder.CreateProjectEntity();
        var milestone = _builder.CreateMilestoneEntity(projectId: project.Id);

        _dbContext.Projects.Add(project);
        _dbContext.Milestones.Add(milestone);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.DeleteAsync(project.Id);

        // Assert
        Assert.True(result);

        // Verify milestones are also deleted (cascade delete)
        var milestonesInDb = _dbContext.Milestones.Where(m => m.ProjectId == project.Id).ToList();
        Assert.Empty(milestonesInDb);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task CreateAsync_WithEmptyName_DoesNotValidateHere()
    {
        // Arrange - validation should happen at controller level
        var request = new CreateProjectRequestV1
        {
            Name = "",
            Content = "Test"
        };

        // Act & Assert - service should accept it (validation is controller's job)
        var result = await _service.CreateAsync(request);
        Assert.NotNull(result);
        Assert.Equal("", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidGuid_ReturnsNull()
    {
        // Arrange
        var invalidId = Guid.Empty;

        // Act
        var result = await _service.GetByIdAsync(invalidId);

        // Assert
        Assert.Null(result);
    }

    #endregion
}
