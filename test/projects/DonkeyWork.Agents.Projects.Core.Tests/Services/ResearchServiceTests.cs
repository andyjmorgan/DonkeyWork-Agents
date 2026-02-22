using DonkeyWork.Agents.Identity.Contracts.Services;
using DonkeyWork.Agents.Notifications.Contracts.Interfaces;
using DonkeyWork.Agents.Persistence;
using DonkeyWork.Agents.Projects.Contracts.Models;
using DonkeyWork.Agents.Projects.Core.Services;
using DonkeyWork.Agents.Projects.Core.Tests.Helpers;
using Microsoft.Extensions.Logging;
using EntityResearchStatus = DonkeyWork.Agents.Persistence.Entities.Research.ResearchStatus;

namespace DonkeyWork.Agents.Projects.Core.Tests.Services;

/// <summary>
/// Unit tests for ResearchService.
/// Tests CRUD operations and business logic without external dependencies.
/// </summary>
public class ResearchServiceTests : IDisposable
{
    private readonly AgentsDbContext _dbContext;
    private readonly IIdentityContext _identityContext;
    private readonly ResearchService _service;
    private readonly Guid _testUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly TestDataBuilder _builder = new();

    public ResearchServiceTests()
    {
        (_dbContext, _identityContext) = MockDbContext.CreateWithIdentityContext();
        var notificationService = new Mock<INotificationService>();
        var logger = new Mock<ILogger<ResearchService>>();
        _service = new ResearchService(_dbContext, _identityContext, notificationService.Object, logger.Object);
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
    }

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidRequest_CreatesResearch()
    {
        // Arrange
        var request = new CreateResearchRequestV1
        {
            Subject = "test-research",
            Content = "Test content"
        };

        // Act
        var result = await _service.CreateAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(request.Subject, result.Subject);
        Assert.Equal(request.Content, result.Content);
        Assert.Equal(ResearchStatus.NotStarted, result.Status);
        Assert.True(result.CreatedAt <= DateTimeOffset.UtcNow);

        // Verify research was created in database
        var researchInDb = await _dbContext.Research.FindAsync(result.Id);
        Assert.NotNull(researchInDb);
        Assert.Equal(_testUserId, researchInDb.UserId);
    }

    [Fact]
    public async Task CreateAsync_WithTags_CreatesResearchWithTags()
    {
        // Arrange
        var request = new CreateResearchRequestV1
        {
            Subject = "test-research",
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

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ExistingResearch_ReturnsDetails()
    {
        // Arrange
        var research = _builder.CreateResearchEntity();
        MockDbContext.SeedResearch(_dbContext, research);

        // Act
        var result = await _service.GetByIdAsync(research.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(research.Id, result.Id);
        Assert.Equal(research.Subject, result.Subject);
        Assert.Equal(research.Content, result.Content);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistent_ReturnsNull()
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
    public async Task ListAsync_ReturnsAllResearch()
    {
        // Arrange
        var research1 = _builder.CreateResearchEntity(subject: "research-1");
        var research2 = _builder.CreateResearchEntity(subject: "research-2");
        var research3 = _builder.CreateResearchEntity(subject: "research-3");
        _dbContext.Research.AddRange(research1, research2, research3);
        await _dbContext.SaveChangesAsync();

        // Act
        var results = await _service.ListAsync();

        // Assert
        Assert.NotNull(results);
        Assert.Equal(3, results.Count);
        Assert.Contains(results, r => r.Id == research1.Id);
        Assert.Contains(results, r => r.Id == research2.Id);
        Assert.Contains(results, r => r.Id == research3.Id);
    }

    [Fact]
    public async Task ListAsync_WithNoResearch_ReturnsEmptyList()
    {
        // Act
        var results = await _service.ListAsync();

        // Assert
        Assert.NotNull(results);
        Assert.Empty(results);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ToCompletedWithoutCompletionNotes_ThrowsInvalidOperationException()
    {
        // Arrange
        var research = _builder.CreateResearchEntity();
        MockDbContext.SeedResearch(_dbContext, research);

        var updateRequest = new UpdateResearchRequestV1
        {
            Subject = research.Subject,
            Status = ResearchStatus.Completed,
            Summary = "Some findings",
            CompletionNotes = null
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateAsync(research.Id, updateRequest));
    }

    [Fact]
    public async Task UpdateAsync_ToCompletedWithoutSummary_ThrowsInvalidOperationException()
    {
        // Arrange
        var research = _builder.CreateResearchEntity();
        MockDbContext.SeedResearch(_dbContext, research);

        var updateRequest = new UpdateResearchRequestV1
        {
            Subject = research.Subject,
            Status = ResearchStatus.Completed,
            Summary = null,
            CompletionNotes = "Done researching"
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateAsync(research.Id, updateRequest));
    }

    [Fact]
    public async Task UpdateAsync_ToCompletedWithSummaryAndNotes_SetsCompletedAt()
    {
        // Arrange
        var research = _builder.CreateResearchEntity();
        MockDbContext.SeedResearch(_dbContext, research);

        var updateRequest = new UpdateResearchRequestV1
        {
            Subject = research.Subject,
            Status = ResearchStatus.Completed,
            Summary = "Key findings: everything works.",
            CompletionNotes = "Research completed successfully."
        };

        // Act
        var result = await _service.UpdateAsync(research.Id, updateRequest);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ResearchStatus.Completed, result.Status);
        Assert.NotNull(result.CompletedAt);
        Assert.Equal("Key findings: everything works.", result.Summary);
        Assert.Equal("Research completed successfully.", result.CompletionNotes);
    }

    [Fact]
    public async Task UpdateAsync_FromCompletedToInProgress_ClearsCompletedAt()
    {
        // Arrange
        var research = _builder.CreateResearchEntity(
            status: EntityResearchStatus.Completed);
        research.Summary = "Findings";
        research.CompletionNotes = "Done";
        research.CompletedAt = DateTimeOffset.UtcNow.AddDays(-1);
        MockDbContext.SeedResearch(_dbContext, research);

        var updateRequest = new UpdateResearchRequestV1
        {
            Subject = research.Subject,
            Status = ResearchStatus.InProgress
        };

        // Act
        var result = await _service.UpdateAsync(research.Id, updateRequest);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ResearchStatus.InProgress, result.Status);
        Assert.Null(result.CompletedAt);
    }

    [Fact]
    public async Task UpdateAsync_ToCancelledWithNotes_SetsCompletedAt()
    {
        // Arrange
        var research = _builder.CreateResearchEntity();
        MockDbContext.SeedResearch(_dbContext, research);

        var updateRequest = new UpdateResearchRequestV1
        {
            Subject = research.Subject,
            Status = ResearchStatus.Cancelled,
            CompletionNotes = "No longer relevant."
        };

        // Act
        var result = await _service.UpdateAsync(research.Id, updateRequest);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ResearchStatus.Cancelled, result.Status);
        Assert.NotNull(result.CompletedAt);
        Assert.Equal("No longer relevant.", result.CompletionNotes);
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistentResearch_ReturnsNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var updateRequest = new UpdateResearchRequestV1
        {
            Subject = "test",
            Status = ResearchStatus.InProgress
        };

        // Act
        var result = await _service.UpdateAsync(nonExistentId, updateRequest);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_ExistingResearch_ReturnsTrue()
    {
        // Arrange
        var research = _builder.CreateResearchEntity();
        MockDbContext.SeedResearch(_dbContext, research);

        // Act
        var result = await _service.DeleteAsync(research.Id);

        // Assert
        Assert.True(result);

        // Verify deleted from database
        var deletedResearch = await _dbContext.Research.FindAsync(research.Id);
        Assert.Null(deletedResearch);
    }

    [Fact]
    public async Task DeleteAsync_NonExistent_ReturnsFalse()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _service.DeleteAsync(nonExistentId);

        // Assert
        Assert.False(result);
    }

    #endregion
}
