using DonkeyWork.Agents.Identity.Contracts.Services;
using DonkeyWork.Agents.Persistence;
using DonkeyWork.Agents.Persistence.Entities.Projects;
using DonkeyWork.Agents.Persistence.Entities.Research;
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
        var (context, _) = CreateWithIdentityContext(databaseName, userId);
        return context;
    }

    /// <summary>
    /// Creates a new in-memory AgentsDbContext along with its IIdentityContext mock.
    /// Use this when you need to pass the same IIdentityContext to services.
    /// </summary>
    public static (AgentsDbContext DbContext, IIdentityContext IdentityContext) CreateWithIdentityContext(string? databaseName = null, Guid? userId = null)
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

        return (context, mockIdentityContext.Object);
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
    /// Seeds the database with a task item.
    /// </summary>
    public static void SeedTaskItem(AgentsDbContext context, TaskItemEntity taskItem)
    {
        context.TaskItems.Add(taskItem);
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

    /// <summary>
    /// Seeds the database with a research item.
    /// </summary>
    public static void SeedResearch(AgentsDbContext context, ResearchEntity research)
    {
        context.Research.Add(research);
        context.SaveChanges();
    }
}
