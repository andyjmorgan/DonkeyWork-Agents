using DonkeyWork.Agents.Persistence;
using DonkeyWork.Agents.Persistence.Entities.Agents;
using DonkeyWork.Agents.Persistence.Entities.Credentials;
using DonkeyWork.Agents.Persistence.Entities.Projects;

namespace DonkeyWork.Agents.Integration.Tests.Helpers;

public class TestDataSeeder
{
    private readonly AgentsDbContext _dbContext;

    public TestDataSeeder(AgentsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    #region Agent Seeding

    public async Task<AgentEntity> SeedAgentAsync(
        Guid userId,
        string name,
        string? description = null)
    {
        var agent = new AgentEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = name,
            Description = description ?? "Test agent description"
        };

        _dbContext.Agents.Add(agent);
        await _dbContext.SaveChangesAsync();
        return agent;
    }

    public async Task<AgentVersionEntity> SeedAgentVersionAsync(
        Guid agentId,
        Guid userId,
        int versionNumber = 1,
        bool isDraft = true)
    {
        var version = new AgentVersionEntity
        {
            Id = Guid.NewGuid(),
            AgentId = agentId,
            UserId = userId,
            VersionNumber = versionNumber,
            IsDraft = isDraft,
            InputSchema = "{}",
            ReactFlowData = "{}",
            NodeConfigurations = "{}"
        };

        _dbContext.AgentVersions.Add(version);
        await _dbContext.SaveChangesAsync();
        return version;
    }

    #endregion

    #region Project Seeding

    public async Task<ProjectEntity> SeedProjectAsync(
        Guid userId,
        string name,
        string? description = null,
        string? body = null,
        ProjectStatus status = ProjectStatus.NotStarted)
    {
        var project = new ProjectEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = name,
            Description = description ?? "Test project description",
            Body = body,
            Status = status
        };

        _dbContext.Projects.Add(project);
        await _dbContext.SaveChangesAsync();
        return project;
    }

    public async Task<MilestoneEntity> SeedMilestoneAsync(
        Guid projectId,
        Guid userId,
        string name,
        string? description = null,
        MilestoneStatus status = MilestoneStatus.NotStarted)
    {
        var milestone = new MilestoneEntity
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            UserId = userId,
            Name = name,
            Description = description ?? "Test milestone description",
            Status = status,
            SortOrder = 0
        };

        _dbContext.Milestones.Add(milestone);
        await _dbContext.SaveChangesAsync();
        return milestone;
    }

    #endregion

    #region API Key Seeding

    public async Task<UserApiKeyEntity> SeedUserApiKeyAsync(
        Guid userId,
        string name,
        string? description = null)
    {
        var apiKey = new UserApiKeyEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = name,
            Description = description ?? "Test API key",
            EncryptedKey = new byte[32]
        };

        _dbContext.UserApiKeys.Add(apiKey);
        await _dbContext.SaveChangesAsync();
        return apiKey;
    }

    #endregion

    public async Task SaveChangesAsync()
    {
        await _dbContext.SaveChangesAsync();
    }
}
