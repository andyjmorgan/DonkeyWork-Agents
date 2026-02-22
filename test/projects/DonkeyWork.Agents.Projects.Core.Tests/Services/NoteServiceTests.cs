using DonkeyWork.Agents.Identity.Contracts.Services;
using DonkeyWork.Agents.Notifications.Contracts.Interfaces;
using DonkeyWork.Agents.Persistence;
using DonkeyWork.Agents.Projects.Contracts.Models;
using DonkeyWork.Agents.Projects.Core.Services;
using DonkeyWork.Agents.Projects.Core.Tests.Helpers;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Projects.Core.Tests.Services;

/// <summary>
/// Unit tests for NoteService.
/// Tests CRUD operations and business logic without external dependencies.
/// </summary>
public class NoteServiceTests : IDisposable
{
    private readonly AgentsDbContext _dbContext;
    private readonly IIdentityContext _identityContext;
    private readonly NoteService _service;
    private readonly Guid _testUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly TestDataBuilder _builder = new();

    public NoteServiceTests()
    {
        (_dbContext, _identityContext) = MockDbContext.CreateWithIdentityContext();
        var notificationService = new Mock<INotificationService>();
        var logger = new Mock<ILogger<NoteService>>();
        _service = new NoteService(_dbContext, _identityContext, notificationService.Object, logger.Object);
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
    }

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidRequest_CreatesNote()
    {
        // Arrange
        var request = new CreateNoteRequestV1
        {
            Title = "test-note",
            Content = "Test content with **markdown**"
        };

        // Act
        var result = await _service.CreateAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(request.Title, result.Title);
        Assert.Equal(request.Content, result.Content);

        // Verify note was created in database
        var noteInDb = await _dbContext.Notes.FindAsync(result.Id);
        Assert.NotNull(noteInDb);
        Assert.Equal(_testUserId, noteInDb.UserId);
    }

    [Fact]
    public async Task CreateAsync_StandaloneNote_HasNullProjectAndMilestone()
    {
        // Arrange
        var request = TestDataBuilder.CreateNoteRequest();

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

        var request = new CreateNoteRequestV1
        {
            Title = "test-note",
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

        var request = new CreateNoteRequestV1
        {
            Title = "test-note",
            MilestoneId = milestone.Id
        };

        // Act
        var result = await _service.CreateAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(milestone.Id, result.MilestoneId);
    }

    [Fact]
    public async Task CreateAsync_WithTags_CreatesNoteWithTags()
    {
        // Arrange
        var request = new CreateNoteRequestV1
        {
            Title = "test-note",
            Tags = new List<TagRequestV1>
            {
                new() { Name = "ideas", Color = "#ff0000" },
                new() { Name = "research", Color = "#00ff00" }
            }
        };

        // Act
        var result = await _service.CreateAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Tags.Count);
        Assert.Contains(result.Tags, t => t.Name == "ideas");
        Assert.Contains(result.Tags, t => t.Name == "research");
    }

    [Fact]
    public async Task CreateAsync_WithNullContent_CreatesSuccessfully()
    {
        // Arrange
        var request = new CreateNoteRequestV1
        {
            Title = "test-note",
            Content = null
        };

        // Act
        var result = await _service.CreateAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Content);
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithExistingNote_ReturnsNote()
    {
        // Arrange
        var note = _builder.CreateNoteEntity();
        MockDbContext.SeedNote(_dbContext, note);

        // Act
        var result = await _service.GetByIdAsync(note.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(note.Id, result.Id);
        Assert.Equal(note.Title, result.Title);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentNote_ReturnsNull()
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
    public async Task ListAsync_WithMultipleNotes_ReturnsAllUserNotes()
    {
        // Arrange
        var note1 = _builder.CreateNoteEntity(title: "note-1");
        var note2 = _builder.CreateNoteEntity(title: "note-2");
        var note3 = _builder.CreateNoteEntity(title: "note-3");
        _dbContext.Notes.AddRange(note1, note2, note3);
        await _dbContext.SaveChangesAsync();

        // Act
        var results = await _service.ListAsync();

        // Assert
        Assert.NotNull(results);
        Assert.Equal(3, results.Count);
    }

    [Fact]
    public async Task ListAsync_WithNoNotes_ReturnsEmptyList()
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
        var note1 = _builder.CreateNoteEntity(title: "oldest");
        await Task.Delay(10);
        var note2 = _builder.CreateNoteEntity(title: "middle");
        await Task.Delay(10);
        var note3 = _builder.CreateNoteEntity(title: "newest");

        _dbContext.Notes.AddRange(note1, note2, note3);
        await _dbContext.SaveChangesAsync();

        // Act
        var results = await _service.ListAsync();

        // Assert
        Assert.Equal(3, results.Count);
        Assert.Equal(note3.Id, results[0].Id);
        Assert.Equal(note2.Id, results[1].Id);
        Assert.Equal(note1.Id, results[2].Id);
    }

    #endregion

    #region GetStandaloneAsync Tests

    [Fact]
    public async Task GetStandaloneAsync_ReturnsOnlyStandaloneNotes()
    {
        // Arrange
        var project = _builder.CreateProjectEntity();
        MockDbContext.SeedProject(_dbContext, project);

        var standaloneNote = _builder.CreateNoteEntity(title: "standalone");
        var projectNote = _builder.CreateNoteEntity(title: "project-note", projectId: project.Id);

        _dbContext.Notes.AddRange(standaloneNote, projectNote);
        await _dbContext.SaveChangesAsync();

        // Act
        var results = await _service.GetStandaloneAsync();

        // Assert
        Assert.NotNull(results);
        Assert.Single(results);
        Assert.Equal("standalone", results[0].Title);
    }

    [Fact]
    public async Task GetStandaloneAsync_ExcludesMilestoneNotes()
    {
        // Arrange
        var project = _builder.CreateProjectEntity();
        var milestone = _builder.CreateMilestoneEntity(projectId: project.Id);
        _dbContext.Projects.Add(project);
        MockDbContext.SeedMilestone(_dbContext, milestone);

        var standaloneNote = _builder.CreateNoteEntity(title: "standalone");
        var milestoneNote = _builder.CreateNoteEntity(title: "milestone-note", milestoneId: milestone.Id);

        _dbContext.Notes.AddRange(standaloneNote, milestoneNote);
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
    public async Task UpdateAsync_WithValidRequest_UpdatesNote()
    {
        // Arrange
        var note = _builder.CreateNoteEntity();
        MockDbContext.SeedNote(_dbContext, note);

        var updateRequest = new UpdateNoteRequestV1
        {
            Title = "updated-note",
            Content = "Updated content",
            SortOrder = 10
        };

        // Act
        var result = await _service.UpdateAsync(note.Id, updateRequest);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(note.Id, result.Id);
        Assert.Equal(updateRequest.Title, result.Title);
        Assert.Equal(updateRequest.Content, result.Content);
        Assert.Equal(updateRequest.SortOrder, result.SortOrder);
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistentNote_ReturnsNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var updateRequest = new UpdateNoteRequestV1
        {
            Title = "test",
            SortOrder = 0
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
        var note = _builder.CreateNoteEntity();
        var originalUpdatedAt = note.UpdatedAt;
        MockDbContext.SeedNote(_dbContext, note);

        await Task.Delay(100);

        var updateRequest = new UpdateNoteRequestV1
        {
            Title = "new-title",
            SortOrder = 0
        };

        // Act
        var result = await _service.UpdateAsync(note.Id, updateRequest);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.UpdatedAt);
        Assert.True(result.UpdatedAt > originalUpdatedAt);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithExistingNote_DeletesNote()
    {
        // Arrange
        var note = _builder.CreateNoteEntity();
        MockDbContext.SeedNote(_dbContext, note);

        // Act
        var result = await _service.DeleteAsync(note.Id);

        // Assert
        Assert.True(result);

        // Verify deleted from database
        var deletedNote = await _dbContext.Notes.FindAsync(note.Id);
        Assert.Null(deletedNote);
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentNote_ReturnsFalse()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _service.DeleteAsync(nonExistentId);

        // Assert
        Assert.False(result);
    }

    // Note: User isolation tests removed - user filtering is handled by DbContext global query filter
    // using IIdentityContext.UserId, not by the userId parameter passed to service methods.
    // User isolation should be tested at the DbContext/integration test level.

    #endregion

    #region Content Truncation and Chunking Tests

    [Fact]
    public async Task ListAsync_WithLongContent_IncludesTruncatedPreview()
    {
        // Arrange
        var longContent = new string('a', 1000);
        var note = _builder.CreateNoteEntity();
        note.Content = longContent;
        _dbContext.Notes.Add(note);
        await _dbContext.SaveChangesAsync();

        // Act
        var results = await _service.ListAsync();

        // Assert
        Assert.Single(results);
        var summary = results[0];
        Assert.NotNull(summary.ContentPreview);
        Assert.Equal(503, summary.ContentPreview.Length); // 500 + "..."
        Assert.EndsWith("...", summary.ContentPreview);
        Assert.Equal(1000, summary.ContentLength);
    }

    [Fact]
    public async Task ListAsync_WithShortContent_PreviewEqualsContent()
    {
        // Arrange
        var shortContent = "Short content";
        var note = _builder.CreateNoteEntity();
        note.Content = shortContent;
        _dbContext.Notes.Add(note);
        await _dbContext.SaveChangesAsync();

        // Act
        var results = await _service.ListAsync();

        // Assert
        Assert.Single(results);
        var summary = results[0];
        Assert.Equal(shortContent, summary.ContentPreview);
        Assert.Equal(shortContent.Length, summary.ContentLength);
    }

    [Fact]
    public async Task ListAsync_WithNullContent_PreviewIsNull()
    {
        // Arrange
        var note = _builder.CreateNoteEntity();
        note.Content = null;
        _dbContext.Notes.Add(note);
        await _dbContext.SaveChangesAsync();

        // Act
        var results = await _service.ListAsync();

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
        var longContent = new string('x', 2000);
        var note = _builder.CreateNoteEntity();
        note.Content = longContent;
        MockDbContext.SeedNote(_dbContext, note);

        // Act
        var result = await _service.GetByIdAsync(note.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(longContent, result.Content);
        Assert.Equal(2000, result.ContentLength);
    }

    [Fact]
    public async Task GetByIdAsync_WithChunking_ReturnsChunkedContent()
    {
        // Arrange
        var content = "Hello World! This is test content.";
        var note = _builder.CreateNoteEntity();
        note.Content = content;
        MockDbContext.SeedNote(_dbContext, note);

        // Act
        var result = await _service.GetByIdAsync(note.Id, contentOffset: 6, contentLength: 5);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("World", result.Content);
        Assert.Equal(content.Length, result.ContentLength);
    }

    [Fact]
    public async Task GetByIdAsync_WithOffsetOnly_ReturnsFromOffset()
    {
        // Arrange
        var content = "Hello World!";
        var note = _builder.CreateNoteEntity();
        note.Content = content;
        MockDbContext.SeedNote(_dbContext, note);

        // Act
        var result = await _service.GetByIdAsync(note.Id, contentOffset: 6);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("World!", result.Content);
        Assert.Equal(12, result.ContentLength);
    }

    #endregion
}
