using System.Net;
using DonkeyWork.Agents.Integration.Tests.Base;
using DonkeyWork.Agents.Integration.Tests.Helpers;
using DonkeyWork.Agents.Integration.Tests.Infrastructure.Authentication;
using DonkeyWork.Agents.Integration.Tests.Infrastructure.Containers;
using DonkeyWork.Agents.Projects.Contracts.Models;

namespace DonkeyWork.Agents.Integration.Tests.Tests.Controllers;

[Trait("Category", "Integration")]
public class NotesControllerTests : ControllerIntegrationTestBase
{
    private const string BaseUrl = "/api/v1/notes";
    private const string ProjectsBaseUrl = "/api/v1/projects";

    public NotesControllerTests(InfrastructureFixture infrastructure)
        : base(infrastructure)
    {
    }

    private async Task<ProjectDetailsV1> CreateProjectAsync()
    {
        return (await PostAsync<ProjectDetailsV1>(ProjectsBaseUrl, TestDataBuilder.CreateProjectRequest()))!;
    }

    #region Create Tests

    [Fact]
    public async Task Create_StandaloneNote_ReturnsCreatedNote()
    {
        // Arrange
        var request = TestDataBuilder.CreateNoteRequest(
            title: "Meeting Notes",
            content: "# Meeting Notes\n\n- Point 1\n- Point 2");

        // Act
        var response = await PostResponseAsync(BaseUrl, request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var note = await response.Content.ReadFromJsonAsync<NoteV1>(JsonOptions);
        Assert.NotNull(note);
        Assert.NotEqual(Guid.Empty, note.Id);
        Assert.Equal("Meeting Notes", note.Title);
        Assert.Contains("Point 1", note.Content);
        Assert.Null(note.ProjectId);
        Assert.Null(note.MilestoneId);
    }

    [Fact]
    public async Task Create_NoteWithProject_ReturnsCreatedNote()
    {
        // Arrange
        var project = await CreateProjectAsync();
        var request = TestDataBuilder.CreateNoteRequest(
            title: "Project Note",
            projectId: project.Id);

        // Act
        var note = await PostAsync<NoteV1>(BaseUrl, request);

        // Assert
        Assert.NotNull(note);
        Assert.Equal(project.Id, note.ProjectId);
    }

    [Fact]
    public async Task Create_NoteWithMarkdownContent_PreservesContent()
    {
        // Arrange
        var markdownContent = """
            # Heading 1
            ## Heading 2

            - Bullet 1
            - Bullet 2

            ```csharp
            var x = 42;
            ```

            > Quote block
            """;
        var request = TestDataBuilder.CreateNoteRequest(
            title: "Markdown Note",
            content: markdownContent);

        // Act
        var note = await PostAsync<NoteV1>(BaseUrl, request);

        // Assert
        Assert.NotNull(note);
        Assert.Contains("# Heading 1", note.Content);
        Assert.Contains("```csharp", note.Content);
        Assert.Contains("> Quote block", note.Content);
    }

    #endregion

    #region Get Tests

    [Fact]
    public async Task Get_ExistingNote_ReturnsNote()
    {
        // Arrange
        var created = await PostAsync<NoteV1>(BaseUrl, TestDataBuilder.CreateNoteRequest());

        // Act
        var note = await GetAsync<NoteV1>($"{BaseUrl}/{created!.Id}");

        // Assert
        Assert.NotNull(note);
        Assert.Equal(created.Id, note.Id);
        Assert.Equal(created.Title, note.Title);
    }

    [Fact]
    public async Task Get_NonExistingNote_ReturnsNotFound()
    {
        // Act
        var response = await GetResponseAsync($"{BaseUrl}/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region List Tests

    [Fact]
    public async Task List_ReturnsAllUserNotes()
    {
        // Arrange
        await PostAsync<NoteV1>(BaseUrl, TestDataBuilder.CreateNoteRequest("Note 1"));
        await PostAsync<NoteV1>(BaseUrl, TestDataBuilder.CreateNoteRequest("Note 2"));
        await PostAsync<NoteV1>(BaseUrl, TestDataBuilder.CreateNoteRequest("Note 3"));

        // Act
        var notes = await GetAsync<List<NoteV1>>(BaseUrl);

        // Assert
        Assert.NotNull(notes);
        Assert.Equal(3, notes.Count);
    }

    [Fact]
    public async Task List_WithNoNotes_ReturnsEmptyList()
    {
        // Act
        var notes = await GetAsync<List<NoteV1>>(BaseUrl);

        // Assert
        Assert.NotNull(notes);
        Assert.Empty(notes);
    }

    [Fact]
    public async Task ListStandalone_ReturnsOnlyStandaloneNotes()
    {
        // Arrange
        var project = await CreateProjectAsync();
        await PostAsync<NoteV1>(BaseUrl, TestDataBuilder.CreateNoteRequest("Standalone 1"));
        await PostAsync<NoteV1>(BaseUrl, TestDataBuilder.CreateNoteRequest("Standalone 2"));
        await PostAsync<NoteV1>(BaseUrl, TestDataBuilder.CreateNoteRequest("Project Note", projectId: project.Id));

        // Act
        var notes = await GetAsync<List<NoteV1>>($"{BaseUrl}/standalone");

        // Assert
        Assert.NotNull(notes);
        Assert.Equal(2, notes.Count);
        Assert.All(notes, n => Assert.Null(n.ProjectId));
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_ExistingNote_ReturnsUpdatedNote()
    {
        // Arrange
        var created = await PostAsync<NoteV1>(BaseUrl, TestDataBuilder.CreateNoteRequest("Original"));

        var updateRequest = TestDataBuilder.UpdateNoteRequest(
            title: "Updated Title",
            content: "Updated content with more details");

        // Act
        var updated = await PutAsync<NoteV1>($"{BaseUrl}/{created!.Id}", updateRequest);

        // Assert
        Assert.NotNull(updated);
        Assert.Equal(created.Id, updated.Id);
        Assert.Equal("Updated Title", updated.Title);
        Assert.Equal("Updated content with more details", updated.Content);
    }

    [Fact]
    public async Task Update_NonExistingNote_ReturnsNotFound()
    {
        // Arrange
        var updateRequest = TestDataBuilder.UpdateNoteRequest();

        // Act
        var response = await PutResponseAsync($"{BaseUrl}/{Guid.NewGuid()}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_PreservesUpdatedAt()
    {
        // Arrange
        var created = await PostAsync<NoteV1>(BaseUrl, TestDataBuilder.CreateNoteRequest());

        // Act
        await Task.Delay(100); // Small delay to ensure timestamp difference
        var beforeUpdate = DateTimeOffset.UtcNow;
        var updated = await PutAsync<NoteV1>($"{BaseUrl}/{created!.Id}", TestDataBuilder.UpdateNoteRequest());
        var afterUpdate = DateTimeOffset.UtcNow;

        // Assert
        Assert.NotNull(updated);
        Assert.NotNull(updated.UpdatedAt);
        Assert.True(updated.UpdatedAt >= beforeUpdate.AddSeconds(-1));
        Assert.True(updated.UpdatedAt <= afterUpdate.AddSeconds(1));
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_ExistingNote_ReturnsNoContent()
    {
        // Arrange
        var created = await PostAsync<NoteV1>(BaseUrl, TestDataBuilder.CreateNoteRequest());

        // Act
        var response = await DeleteAsync($"{BaseUrl}/{created!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify deletion
        var getResponse = await GetResponseAsync($"{BaseUrl}/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_NonExistingNote_ReturnsNotFound()
    {
        // Act
        var response = await DeleteAsync($"{BaseUrl}/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region User Isolation Tests

    [Fact]
    public async Task Get_NoteBelongingToAnotherUser_ReturnsNotFound()
    {
        // Arrange - Create note as user 1
        var user1 = TestUser.CreateRandom();
        SetTestUser(user1);
        var created = await PostAsync<NoteV1>(BaseUrl, TestDataBuilder.CreateNoteRequest());

        // Act - Try to get as user 2
        var user2 = TestUser.CreateRandom();
        SetTestUser(user2);
        var response = await GetResponseAsync($"{BaseUrl}/{created!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task List_OnlyReturnsNotesForCurrentUser()
    {
        // Arrange - Create notes for user 1
        var user1 = TestUser.CreateRandom();
        SetTestUser(user1);
        await PostAsync<NoteV1>(BaseUrl, TestDataBuilder.CreateNoteRequest("User1 Note"));

        var user2 = TestUser.CreateRandom();
        SetTestUser(user2);
        await PostAsync<NoteV1>(BaseUrl, TestDataBuilder.CreateNoteRequest("User2 Note"));

        // Act - List as user 2
        var notes = await GetAsync<List<NoteV1>>(BaseUrl);

        // Assert
        Assert.NotNull(notes);
        Assert.Single(notes);
        Assert.Equal("User2 Note", notes[0].Title);
    }

    [Fact]
    public async Task Delete_NoteBelongingToAnotherUser_ReturnsNotFound()
    {
        // Arrange - Create note as user 1
        var user1 = TestUser.CreateRandom();
        SetTestUser(user1);
        var created = await PostAsync<NoteV1>(BaseUrl, TestDataBuilder.CreateNoteRequest());

        // Act - Try to delete as user 2
        var user2 = TestUser.CreateRandom();
        SetTestUser(user2);
        var response = await DeleteAsync($"{BaseUrl}/{created!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        // Verify note still exists for user 1
        SetTestUser(user1);
        var getResponse = await GetResponseAsync($"{BaseUrl}/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
    }

    #endregion
}
