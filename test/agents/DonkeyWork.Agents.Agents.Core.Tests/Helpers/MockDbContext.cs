using DonkeyWork.Agents.Identity.Contracts.Services;
using DonkeyWork.Agents.Persistence;
using DonkeyWork.Agents.Persistence.Entities.Agents;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace DonkeyWork.Agents.Agents.Core.Tests.Helpers;

/// <summary>
/// Helper class for creating in-memory database contexts for testing.
/// </summary>
public static class MockDbContext
{
    /// <summary>
    /// Creates a new in-memory AgentsDbContext for testing.
    /// Each database has a unique name to ensure isolation between tests.
    /// </summary>
    public static AgentsDbContext Create(string databaseName = null!, Guid? userId = null)
    {
        databaseName ??= Guid.NewGuid().ToString();
        userId ??= Guid.Parse("11111111-1111-1111-1111-111111111111");

        var options = new DbContextOptionsBuilder<AgentsDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        // Mock IIdentityContext to provide a user ID for query filtering
        var mockIdentityContext = new Mock<IIdentityContext>();
        mockIdentityContext.Setup(x => x.UserId).Returns(userId.Value);
        mockIdentityContext.Setup(x => x.Email).Returns("test@example.com");
        mockIdentityContext.Setup(x => x.Name).Returns("Test User");
        mockIdentityContext.Setup(x => x.Username).Returns("testuser");

        var context = new AgentsDbContext(options, mockIdentityContext.Object);

        return context;
    }

    /// <summary>
    /// Seeds the database with test data.
    /// </summary>
    public static void SeedAgent(AgentsDbContext context, AgentEntity agent, AgentVersionEntity? version = null)
    {
        context.Agents.Add(agent);

        if (version != null)
        {
            context.AgentVersions.Add(version);
        }

        context.SaveChanges();
    }

    /// <summary>
    /// Seeds the database with multiple agents and versions.
    /// </summary>
    public static void SeedMultipleAgents(AgentsDbContext context, params (AgentEntity agent, AgentVersionEntity[] versions)[] data)
    {
        foreach (var (agent, versions) in data)
        {
            context.Agents.Add(agent);
            context.AgentVersions.AddRange(versions);
        }

        context.SaveChanges();
    }
}
