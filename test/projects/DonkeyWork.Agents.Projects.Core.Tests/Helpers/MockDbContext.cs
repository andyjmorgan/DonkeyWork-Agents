using DonkeyWork.Agents.Identity.Contracts.Services;
using DonkeyWork.Agents.Persistence;
using DonkeyWork.Agents.Persistence.Entities.Projects;
using Microsoft.EntityFrameworkCore;

namespace DonkeyWork.Agents.Projects.Core.Tests.Helpers;

/// <summary>
/// Helper class for creating in-memory database contexts for testing.
/// </summary>
public static class MockDbContext
{
    /// <summary>
    /// Creates a new in-memory AgentsDbContext for testing.
    /// Each database has a unique name to ensure isolation between tests.
    /// </summary>
    public static AgentsDbContext Create(string? databaseName = null, Guid? userId = null)
    {
        databaseName ??= Guid.NewGuid().ToString();
        userId ??= Guid.Parse("11111111-1111-1111-1111-111111111111");

        var options = new DbContextOptionsBuilder<AgentsDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        var mockIdentityContext = new Mock<IIdentityContext>();
        mockIdentityContext.Setup(x => x.UserId).Returns(userId.Value);
        mockIdentityContext.Setup(x => x.Email).Returns("test@example.com");
        mockIdentityContext.Setup(x => x.Name).Returns("Test User");
        mockIdentityContext.Setup(x => x.Username).Returns("testuser");

        var context = new AgentsDbContext(options, mockIdentityContext.Object);

        return context;
    }

    /// <summary>
    /// Seeds the database with a project.
    /// </summary>
    public static void SeedProject(AgentsDbContext context, ProjectEntity project)
    {
        context.Projects.Add(project);
        context.SaveChanges();
    }

    /// <summary>
    /// Seeds the database with a milestone.
    /// </summary>
    public static void SeedMilestone(AgentsDbContext context, MilestoneEntity milestone)
    {
        context.Milestones.Add(milestone);
        context.SaveChanges();
    }

    /// <summary>
    /// Seeds the database with a todo.
    /// </summary>
    public static void SeedTodo(AgentsDbContext context, TodoEntity todo)
    {
        context.Todos.Add(todo);
        context.SaveChanges();
    }

    /// <summary>
    /// Seeds the database with a note.
    /// </summary>
    public static void SeedNote(AgentsDbContext context, NoteEntity note)
    {
        context.Notes.Add(note);
        context.SaveChanges();
    }
}
