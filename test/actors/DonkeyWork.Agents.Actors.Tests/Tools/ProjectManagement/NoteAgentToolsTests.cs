using System.Text.Json;
using DonkeyWork.Agents.Actors.Core.Tools.ProjectManagement;
using DonkeyWork.Agents.Projects.Contracts.Models;
using DonkeyWork.Agents.Projects.Contracts.Services;
using Moq;
using Xunit;

namespace DonkeyWork.Agents.Actors.Tests.Tools.ProjectManagement;

public class NoteAgentToolsTests
{
    private readonly Mock<INoteService> _noteService = new();
    private readonly NoteAgentTools _tools;

    public NoteAgentToolsTests()
    {
        _tools = new NoteAgentTools(_noteService.Object);
    }

    #region ListNotes Tests

    [Fact]
    public async Task ListNotes_ReturnsJsonResult()
    {
        // Arrange
        _noteService.Setup(x => x.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<NoteSummaryV1> { new() { Id = Guid.NewGuid(), Title = "Note 1" } });

        // Act
        var result = await _tools.ListNotes();

        // Assert
        Assert.False(result.IsError);
        var deserialized = JsonSerializer.Deserialize<List<JsonElement>>(result.Content);
        Assert.NotNull(deserialized);
        Assert.Single(deserialized);
    }

    #endregion

    #region ListNotesByProject Tests

    [Fact]
    public async Task ListNotesByProject_DelegatesToService()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        _noteService.Setup(x => x.GetByProjectIdAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<NoteSummaryV1>());

        // Act
        var result = await _tools.ListNotesByProject(projectId);

        // Assert
        Assert.False(result.IsError);
        _noteService.Verify(x => x.GetByProjectIdAsync(projectId, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region ListNotesByMilestone Tests

    [Fact]
    public async Task ListNotesByMilestone_DelegatesToService()
    {
        // Arrange
        var milestoneId = Guid.NewGuid();
        _noteService.Setup(x => x.GetByMilestoneIdAsync(milestoneId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<NoteSummaryV1>());

        // Act
        var result = await _tools.ListNotesByMilestone(milestoneId);

        // Assert
        Assert.False(result.IsError);
        _noteService.Verify(x => x.GetByMilestoneIdAsync(milestoneId, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region ListNotesByResearch Tests

    [Fact]
    public async Task ListNotesByResearch_DelegatesToService()
    {
        // Arrange
        var researchId = Guid.NewGuid();
        _noteService.Setup(x => x.GetByResearchIdAsync(researchId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<NoteSummaryV1>());

        // Act
        var result = await _tools.ListNotesByResearch(researchId);

        // Assert
        Assert.False(result.IsError);
        _noteService.Verify(x => x.GetByResearchIdAsync(researchId, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetNote Tests

    [Fact]
    public async Task GetNote_WhenFound_ReturnsJsonResult()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var note = new NoteV1 { Id = noteId, Title = "Test Note" };
        _noteService.Setup(x => x.GetByIdAsync(noteId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(note);

        // Act
        var result = await _tools.GetNote(noteId);

        // Assert
        Assert.False(result.IsError);
        Assert.Contains("Test Note", result.Content);
    }

    [Fact]
    public async Task GetNote_WhenNotFound_ReturnsNotFoundError()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        _noteService.Setup(x => x.GetByIdAsync(noteId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NoteV1?)null);

        // Act
        var result = await _tools.GetNote(noteId);

        // Assert
        Assert.True(result.IsError);
        Assert.Contains("not found", result.Content);
    }

    #endregion

    #region CreateNote Tests

    [Fact]
    public async Task CreateNote_DelegatesToService()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var created = new NoteV1 { Id = Guid.NewGuid(), Title = "New Note" };
        _noteService.Setup(x => x.CreateAsync(It.IsAny<CreateNoteRequestV1>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(created);

        // Act
        var result = await _tools.CreateNote("New Note", "Content", projectId: projectId);

        // Assert
        Assert.False(result.IsError);
        _noteService.Verify(x => x.CreateAsync(
            It.Is<CreateNoteRequestV1>(r => r.Title == "New Note" && r.Content == "Content" && r.ProjectId == projectId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region UpdateNote Tests

    [Fact]
    public async Task UpdateNote_WhenFound_ReturnsJsonResult()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var updated = new NoteV1 { Id = noteId, Title = "Updated" };
        _noteService.Setup(x => x.UpdateAsync(noteId, It.IsAny<UpdateNoteRequestV1>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updated);

        // Act
        var result = await _tools.UpdateNote(noteId, "Updated");

        // Assert
        Assert.False(result.IsError);
        Assert.Contains(noteId.ToString(), result.Content);
        Assert.Contains("updated", result.Content);
    }

    [Fact]
    public async Task UpdateNote_WhenNotFound_ReturnsNotFoundError()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        _noteService.Setup(x => x.UpdateAsync(noteId, It.IsAny<UpdateNoteRequestV1>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((NoteV1?)null);

        // Act
        var result = await _tools.UpdateNote(noteId, "Updated");

        // Assert
        Assert.True(result.IsError);
        Assert.Contains("not found", result.Content);
    }

    #endregion

    #region DeleteNote Tests

    [Fact]
    public async Task DeleteNote_WhenDeleted_ReturnsSuccess()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        _noteService.Setup(x => x.DeleteAsync(noteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _tools.DeleteNote(noteId);

        // Assert
        Assert.False(result.IsError);
        Assert.Contains("deleted successfully", result.Content);
    }

    [Fact]
    public async Task DeleteNote_WhenNotFound_ReturnsNotFoundError()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        _noteService.Setup(x => x.DeleteAsync(noteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _tools.DeleteNote(noteId);

        // Assert
        Assert.True(result.IsError);
        Assert.Contains("not found", result.Content);
    }

    #endregion
}
