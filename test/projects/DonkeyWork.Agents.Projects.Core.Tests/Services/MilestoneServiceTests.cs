using DonkeyWork.Agents.Identity.Contracts.Services;
using DonkeyWork.Agents.Notifications.Contracts.Interfaces;
using DonkeyWork.Agents.Persistence;
using DonkeyWork.Agents.Projects.Contracts.Models;
using DonkeyWork.Agents.Projects.Contracts.Services;
using DonkeyWork.Agents.Projects.Core.Services;
using DonkeyWork.Agents.Projects.Core.Tests.Helpers;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Projects.Core.Tests.Services;

/// <summary>
/// Unit tests for MilestoneService.
/// Tests CRUD operations and business logic without external dependencies.
/// </summary>
public class MilestoneServiceTests : IDisposable
{
    private readonly AgentsDbContext _dbContext;
    private readonly IIdentityContext _identityContext;
    private readonly MilestoneService _service;
    private readonly Guid _testUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly TestDataBuilder _builder = new();

    public MilestoneServiceTests()
    {
        (_dbContext, _identityContext) = MockDbContext.CreateWithIdentityContext();
        var notificationService = new Mock<INotificationService>();
        var projectNotificationService = new Mock<IProjectNotificationService>();
        var logger = new Mock<ILogger<MilestoneService>>();
        _service = new MilestoneService(_dbContext, _identityContext, notificationService.Object, projectNotificationService.Object, logger.Object);
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
    }

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidRequest_CreatesMilestone()
    {
        // Arrange
        var project = _builder.CreateProjectEntity();
        MockDbContext.SeedProject(_dbContext, project);

        var request = new CreateMilestoneRequestV1
        {
            Name = "test-milestone",
            Content = "Test content",
            DueDate = DateTimeOffset.UtcNow.AddDays(30)
        };

        // Act
        var result = await _service.CreateAsync(project.Id, request);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(request.Name, result.Name);
        Assert.Equal(request.Content, result.Content);
        Assert.Equal(project.Id, result.ProjectId);
        Assert.Equal(MilestoneStatus.NotStarted, result.Status);

        // Verify milestone was created in database
        var milestoneInDb = await _dbContext.Milestones.FindAsync(result.Id);
        Assert.NotNull(milestoneInDb);
        Assert.Equal(_testUserId, milestoneInDb.UserId);
    }

    [Fact]
    public async Task CreateAsync_WithNonExistentProject_ReturnsNull()
    {
        // Arrange
        var nonExistentProjectId = Guid.NewGuid();
        var request = TestDataBuilder.CreateMilestoneRequest();

        // Act
        var result = await _service.CreateAsync(nonExistentProjectId, request);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_WithTags_CreatesMilestoneWithTags()
    {
        // Arrange
        var project = _builder.CreateProjectEntity();
        MockDbContext.SeedProject(_dbContext, project);

        var request = new CreateMilestoneRequestV1
        {
            Name = "test-milestone",
            Tags = new List<TagRequestV1>
            {
                new() { Name = "tag1", Color = "#ff0000" },
                new() { Name = "tag2", Color = "#00ff00" }
            }
        };

        // Act
        var result = await _service.CreateAsync(project.Id, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Tags.Count);
        Assert.Contains(result.Tags, t => t.Name == "tag1");
        Assert.Contains(result.Tags, t => t.Name == "tag2");
    }

    [Fact]
    public async Task CreateAsync_MultipleMilestones_UsesSortOrderFromRequest()
    {
        // Arrange
        var project = _builder.CreateProjectEntity();
        MockDbContext.SeedProject(_dbContext, project);

        var request1 = new CreateMilestoneRequestV1 { Name = "milestone-1", SortOrder = 0 };
        var request2 = new CreateMilestoneRequestV1 { Name = "milestone-2", SortOrder = 1 };

        // Act
        var result1 = await _service.CreateAsync(project.Id, request1);
        var result2 = await _service.CreateAsync(project.Id, request2);

        // Assert
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Equal(0, result1.SortOrder);
        Assert.Equal(1, result2.SortOrder);
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithExistingMilestone_ReturnsMilestone()
    {
        // Arrange
        var project = _builder.CreateProjectEntity();
        var milestone = _builder.CreateMilestoneEntity(projectId: project.Id);
        _dbContext.Projects.Add(project);
        MockDbContext.SeedMilestone(_dbContext, milestone);

        // Act
        var result = await _service.GetByIdAsync(milestone.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(milestone.Id, result.Id);
        Assert.Equal(milestone.Name, result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentMilestone_ReturnsNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _service.GetByIdAsync(nonExistentId);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetByProjectIdAsync Tests

    [Fact]
    public async Task GetByProjectIdAsync_WithMultipleMilestones_ReturnsAllMilestones()
    {
        // Arrange
        var project = _builder.CreateProjectEntity();
        var milestone1 = _builder.CreateMilestoneEntity(projectId: project.Id, name: "milestone-1", sortOrder: 0);
        var milestone2 = _builder.CreateMilestoneEntity(projectId: project.Id, name: "milestone-2", sortOrder: 1);
        var milestone3 = _builder.CreateMilestoneEntity(projectId: project.Id, name: "milestone-3", sortOrder: 2);

        _dbContext.Projects.Add(project);
        _dbContext.Milestones.AddRange(milestone1, milestone2, milestone3);
        await _dbContext.SaveChangesAsync();

        // Act
        var results = await _service.GetByProjectIdAsync(project.Id);

        // Assert
        Assert.NotNull(results);
        Assert.Equal(3, results.Count);
    }

    [Fact]
    public async Task GetByProjectIdAsync_WithNoMilestones_ReturnsEmptyList()
    {
        // Arrange
        var project = _builder.CreateProjectEntity();
        MockDbContext.SeedProject(_dbContext, project);

        // Act
        var results = await _service.GetByProjectIdAsync(project.Id);

        // Assert
        Assert.NotNull(results);
        Assert.Empty(results);
    }

    [Fact]
    public async Task GetByProjectIdAsync_OrdersBySortOrder()
    {
        // Arrange
        var project = _builder.CreateProjectEntity();
        var milestone1 = _builder.CreateMilestoneEntity(projectId: project.Id, name: "third", sortOrder: 2);
        var milestone2 = _builder.CreateMilestoneEntity(projectId: project.Id, name: "first", sortOrder: 0);
        var milestone3 = _builder.CreateMilestoneEntity(projectId: project.Id, name: "second", sortOrder: 1);

        _dbContext.Projects.Add(project);
        _dbContext.Milestones.AddRange(milestone1, milestone2, milestone3);
        await _dbContext.SaveChangesAsync();

        // Act
        var results = await _service.GetByProjectIdAsync(project.Id);

        // Assert
        Assert.Equal(3, results.Count);
        Assert.Equal("first", results[0].Name);
        Assert.Equal("second", results[1].Name);
        Assert.Equal("third", results[2].Name);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidRequest_UpdatesMilestone()
    {
        // Arrange
        var project = _builder.CreateProjectEntity();
        var milestone = _builder.CreateMilestoneEntity(projectId: project.Id);
        _dbContext.Projects.Add(project);
        MockDbContext.SeedMilestone(_dbContext, milestone);

        var updateRequest = new UpdateMilestoneRequestV1
        {
            Name = "updated-milestone",
            Content = "Updated content",
            Status = MilestoneStatus.InProgress,
            SortOrder = 5,
            DueDate = DateTimeOffset.UtcNow.AddDays(60)
        };

        // Act
        var result = await _service.UpdateAsync(milestone.Id, updateRequest);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(milestone.Id, result.Id);
        Assert.Equal(updateRequest.Name, result.Name);
        Assert.Equal(updateRequest.Content, result.Content);
        Assert.Equal(updateRequest.Status, result.Status);
        Assert.Equal(updateRequest.SortOrder, result.SortOrder);
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistentMilestone_ReturnsNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var updateRequest = new UpdateMilestoneRequestV1
        {
            Name = "test",
            Status = MilestoneStatus.InProgress,
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
        var project = _builder.CreateProjectEntity();
        var milestone = _builder.CreateMilestoneEntity(projectId: project.Id);
        _dbContext.Projects.Add(project);
        MockDbContext.SeedMilestone(_dbContext, milestone);

        var updateRequest = new UpdateMilestoneRequestV1
        {
            Name = milestone.Name,
            Status = MilestoneStatus.Completed,
            SortOrder = milestone.SortOrder,
            CompletionNotes = null
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateAsync(milestone.Id, updateRequest));
    }

    [Fact]
    public async Task UpdateAsync_ToCancelledWithoutNotes_ThrowsInvalidOperationException()
    {
        // Arrange
        var project = _builder.CreateProjectEntity();
        var milestone = _builder.CreateMilestoneEntity(projectId: project.Id);
        _dbContext.Projects.Add(project);
        MockDbContext.SeedMilestone(_dbContext, milestone);

        var updateRequest = new UpdateMilestoneRequestV1
        {
            Name = milestone.Name,
            Status = MilestoneStatus.Cancelled,
            SortOrder = milestone.SortOrder,
            CompletionNotes = null
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateAsync(milestone.Id, updateRequest));
    }

    [Fact]
    public async Task UpdateAsync_ToCompletedWithNotes_SetsCompletedAt()
    {
        // Arrange
        var project = _builder.CreateProjectEntity();
        var milestone = _builder.CreateMilestoneEntity(projectId: project.Id);
        _dbContext.Projects.Add(project);
        MockDbContext.SeedMilestone(_dbContext, milestone);

        var updateRequest = new UpdateMilestoneRequestV1
        {
            Name = milestone.Name,
            Status = MilestoneStatus.Completed,
            SortOrder = milestone.SortOrder,
            CompletionNotes = "Milestone achieved successfully."
        };

        // Act
        var result = await _service.UpdateAsync(milestone.Id, updateRequest);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(MilestoneStatus.Completed, result.Status);
        Assert.NotNull(result.CompletedAt);
        Assert.Equal("Milestone achieved successfully.", result.CompletionNotes);
    }

    [Fact]
    public async Task UpdateAsync_FromCompletedToInProgress_ClearsCompletedAt()
    {
        // Arrange
        var project = _builder.CreateProjectEntity();
        var milestone = _builder.CreateMilestoneEntity(
            projectId: project.Id,
            status: Persistence.Entities.Projects.MilestoneStatus.Completed);
        milestone.CompletionNotes = "Done";
        milestone.CompletedAt = DateTimeOffset.UtcNow.AddDays(-1);
        _dbContext.Projects.Add(project);
        MockDbContext.SeedMilestone(_dbContext, milestone);

        var updateRequest = new UpdateMilestoneRequestV1
        {
            Name = milestone.Name,
            Status = MilestoneStatus.InProgress,
            SortOrder = milestone.SortOrder
        };

        // Act
        var result = await _service.UpdateAsync(milestone.Id, updateRequest);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(MilestoneStatus.InProgress, result.Status);
        Assert.Null(result.CompletedAt);
    }

    [Fact]
    public async Task UpdateAsync_ToInProgress_DoesNotRequireNotes()
    {
        // Arrange
        var project = _builder.CreateProjectEntity();
        var milestone = _builder.CreateMilestoneEntity(projectId: project.Id);
        _dbContext.Projects.Add(project);
        MockDbContext.SeedMilestone(_dbContext, milestone);

        var updateRequest = new UpdateMilestoneRequestV1
        {
            Name = milestone.Name,
            Status = MilestoneStatus.InProgress,
            SortOrder = milestone.SortOrder,
            CompletionNotes = null
        };

        // Act
        var result = await _service.UpdateAsync(milestone.Id, updateRequest);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(MilestoneStatus.InProgress, result.Status);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithExistingMilestone_DeletesMilestone()
    {
        // Arrange
        var project = _builder.CreateProjectEntity();
        var milestone = _builder.CreateMilestoneEntity(projectId: project.Id);
        _dbContext.Projects.Add(project);
        MockDbContext.SeedMilestone(_dbContext, milestone);

        // Act
        var result = await _service.DeleteAsync(milestone.Id);

        // Assert
        Assert.True(result);

        // Verify deleted from database
        var deletedMilestone = await _dbContext.Milestones.FindAsync(milestone.Id);
        Assert.Null(deletedMilestone);
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentMilestone_ReturnsFalse()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _service.DeleteAsync(nonExistentId);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Content Truncation and Chunking Tests

    [Fact]
    public async Task GetByProjectIdAsync_WithLongContent_IncludesTruncatedPreview()
    {
        // Arrange
        var project = _builder.CreateProjectEntity();
        var milestone = _builder.CreateMilestoneEntity(projectId: project.Id);
        milestone.Content = new string('a', 1000);
        _dbContext.Projects.Add(project);
        MockDbContext.SeedMilestone(_dbContext, milestone);

        // Act
        var results = await _service.GetByProjectIdAsync(project.Id);

        // Assert
        Assert.Single(results);
        var summary = results[0];
        Assert.NotNull(summary.ContentPreview);
        Assert.Equal(63, summary.ContentPreview.Length); // 60 + "..."
        Assert.EndsWith("...", summary.ContentPreview);
        Assert.Equal(1000, summary.ContentLength);
    }

    [Fact]
    public async Task GetByProjectIdAsync_WithShortContent_PreviewEqualsContent()
    {
        // Arrange
        var project = _builder.CreateProjectEntity();
        var milestone = _builder.CreateMilestoneEntity(projectId: project.Id);
        milestone.Content = "Short content";
        _dbContext.Projects.Add(project);
        MockDbContext.SeedMilestone(_dbContext, milestone);

        // Act
        var results = await _service.GetByProjectIdAsync(project.Id);

        // Assert
        Assert.Single(results);
        var summary = results[0];
        Assert.Equal("Short content", summary.ContentPreview);
        Assert.Equal(13, summary.ContentLength);
    }

    [Fact]
    public async Task GetByProjectIdAsync_WithNullContent_PreviewIsNull()
    {
        // Arrange
        var project = _builder.CreateProjectEntity();
        var milestone = _builder.CreateMilestoneEntity(projectId: project.Id);
        milestone.Content = null;
        _dbContext.Projects.Add(project);
        MockDbContext.SeedMilestone(_dbContext, milestone);

        // Act
        var results = await _service.GetByProjectIdAsync(project.Id);

        // Assert
        Assert.Single(results);
        var summary = results[0];
        Assert.Null(summary.ContentPreview);
        Assert.Equal(0, summary.ContentLength);
    }

    [Fact]
    public async Task GetByIdAsync_WithoutChunking_ReturnsFullContent()
    {
        // Arrange
        var project = _builder.CreateProjectEntity();
        var milestone = _builder.CreateMilestoneEntity(projectId: project.Id);
        var longContent = new string('x', 2000);
        milestone.Content = longContent;
        _dbContext.Projects.Add(project);
        MockDbContext.SeedMilestone(_dbContext, milestone);

        // Act
        var result = await _service.GetByIdAsync(milestone.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(longContent, result.Content);
        Assert.Equal(2000, result.ContentLength);
    }

    [Fact]
    public async Task GetByIdAsync_WithChunking_ReturnsChunkedContent()
    {
        // Arrange
        var project = _builder.CreateProjectEntity();
        var milestone = _builder.CreateMilestoneEntity(projectId: project.Id);
        var content = "Hello World! This is test content.";
        milestone.Content = content;
        _dbContext.Projects.Add(project);
        MockDbContext.SeedMilestone(_dbContext, milestone);

        // Act
        var result = await _service.GetByIdAsync(milestone.Id, contentOffset: 6, contentLength: 5);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("World", result.Content);
        Assert.Equal(content.Length, result.ContentLength);
    }

    [Fact]
    public async Task GetByIdAsync_WithOffsetOnly_ReturnsFromOffset()
    {
        // Arrange
        var project = _builder.CreateProjectEntity();
        var milestone = _builder.CreateMilestoneEntity(projectId: project.Id);
        var content = "Hello World!";
        milestone.Content = content;
        _dbContext.Projects.Add(project);
        MockDbContext.SeedMilestone(_dbContext, milestone);

        // Act
        var result = await _service.GetByIdAsync(milestone.Id, contentOffset: 6);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("World!", result.Content);
        Assert.Equal(12, result.ContentLength);
    }

    #endregion
}
