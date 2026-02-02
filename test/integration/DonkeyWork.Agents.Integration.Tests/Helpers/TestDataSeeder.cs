using System.Text.Json;
using DonkeyWork.Agents.Agents.Contracts.Models.ReactFlow;
using DonkeyWork.Agents.Agents.Contracts.Nodes.Configurations;
using DonkeyWork.Agents.Agents.Contracts.Nodes.Enums;
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
        var startNodeId = Guid.NewGuid();
        var endNodeId = Guid.NewGuid();

        var reactFlowData = new ReactFlowData
        {
            Nodes =
            [
                new ReactFlowNode
                {
                    Id = startNodeId,
                    Type = "schemaNode",
                    Position = new ReactFlowPosition { X = 100, Y = 100 },
                    Data = new ReactFlowNodeData { NodeType = NodeType.Start, Label = "start_1", DisplayName = "start_1" }
                },
                new ReactFlowNode
                {
                    Id = endNodeId,
                    Type = "schemaNode",
                    Position = new ReactFlowPosition { X = 100, Y = 250 },
                    Data = new ReactFlowNodeData { NodeType = NodeType.End, Label = "end_1", DisplayName = "end_1" }
                }
            ],
            Edges =
            [
                new ReactFlowEdge { Id = Guid.NewGuid(), Source = startNodeId, Target = endNodeId }
            ],
            Viewport = new ReactFlowViewport { X = 0, Y = 0, Zoom = 1 }
        };

        var nodeConfigurations = new Dictionary<Guid, NodeConfiguration>
        {
            [startNodeId] = new StartNodeConfiguration { Name = "start_1", InputSchema = JsonSerializer.SerializeToElement(new { type = "object" }) },
            [endNodeId] = new EndNodeConfiguration { Name = "end_1" }
        };

        var version = new AgentVersionEntity
        {
            Id = Guid.NewGuid(),
            AgentId = agentId,
            UserId = userId,
            VersionNumber = versionNumber,
            IsDraft = isDraft,
            InputSchema = JsonDocument.Parse("{}"),
            ReactFlowData = reactFlowData,
            NodeConfigurations = nodeConfigurations
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
        string? content = null,
        ProjectStatus status = ProjectStatus.NotStarted)
    {
        var project = new ProjectEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = name,
            Content = content ?? "Test project content",
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
        string? content = null,
        MilestoneStatus status = MilestoneStatus.NotStarted)
    {
        var milestone = new MilestoneEntity
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            UserId = userId,
            Name = name,
            Content = content ?? "Test milestone content",
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
