using System.Text.Json;
using DonkeyWork.Agents.Agents.Contracts.Models;
using DonkeyWork.Agents.Agents.Contracts.Models.ReactFlow;
using DonkeyWork.Agents.Agents.Contracts.Nodes.Configurations;
using DonkeyWork.Agents.Agents.Contracts.Nodes.Enums;
using DonkeyWork.Agents.Agents.Contracts.Services;
using DonkeyWork.Agents.Persistence;
using DonkeyWork.Agents.Persistence.Entities.Agents;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Agents.Core.Services;

public class AgentService : IAgentService
{
    private readonly AgentsDbContext _dbContext;
    private readonly ILogger<AgentService> _logger;

    public AgentService(AgentsDbContext dbContext, ILogger<AgentService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<CreateAgentResponseV1> CreateAsync(CreateAgentRequestV1 request, Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating agent for user {UserId} with name {Name}", userId, request.Name);

        var agentId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var startNodeId = Guid.NewGuid();
        var endNodeId = Guid.NewGuid();

        // Create default template: Start -> End
        var defaultInputSchema = JsonDocument.Parse("""
            {
                "type": "object",
                "properties": {
                    "input": { "type": "string" }
                },
                "required": ["input"]
            }
            """);

        var defaultReactFlowData = new ReactFlowData
        {
            Nodes =
            [
                new ReactFlowNode
                {
                    Id = startNodeId,
                    Type = "schemaNode",
                    Position = new ReactFlowPosition { X = 250, Y = 50 },
                    Data = new ReactFlowNodeData
                    {
                        NodeType = NodeType.Start,
                        Label = "start",
                        DisplayName = "Start",
                        Icon = "play",
                        Color = "green",
                        HasInputHandle = false,
                        CanDelete = false
                    }
                },
                new ReactFlowNode
                {
                    Id = endNodeId,
                    Type = "schemaNode",
                    Position = new ReactFlowPosition { X = 250, Y = 250 },
                    Data = new ReactFlowNodeData
                    {
                        NodeType = NodeType.End,
                        Label = "end",
                        DisplayName = "End",
                        Icon = "flag",
                        Color = "orange",
                        HasOutputHandle = false,
                        CanDelete = false
                    }
                }
            ],
            Edges =
            [
                new ReactFlowEdge
                {
                    Id = Guid.NewGuid(),
                    Source = startNodeId,
                    Target = endNodeId
                }
            ],
            Viewport = new ReactFlowViewport { X = 0, Y = 0, Zoom = 1 }
        };

        var defaultNodeConfigurations = new Dictionary<Guid, NodeConfiguration>
        {
            [startNodeId] = new StartNodeConfiguration
            {
                Name = "start",
                InputSchema = defaultInputSchema.RootElement.Clone()
            },
            [endNodeId] = new EndNodeConfiguration
            {
                Name = "end"
            }
        };

        var agent = new AgentEntity
        {
            Id = agentId,
            UserId = userId,
            Name = request.Name,
            Description = request.Description,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var version = new AgentVersionEntity
        {
            Id = versionId,
            UserId = userId,
            AgentId = agentId,
            VersionNumber = 1,
            IsDraft = true,
            InputSchema = defaultInputSchema,
            ReactFlowData = defaultReactFlowData,
            NodeConfigurations = defaultNodeConfigurations,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.Agents.Add(agent);
        _dbContext.AgentVersions.Add(version);

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created agent {AgentId} with initial draft version {VersionId}", agentId, versionId);

        return new CreateAgentResponseV1
        {
            Id = agentId,
            Name = agent.Name,
            Description = agent.Description,
            VersionId = versionId,
            CreatedAt = agent.CreatedAt
        };
    }

    public async Task<GetAgentResponseV1?> GetByIdAsync(Guid agentId, Guid userId, CancellationToken cancellationToken = default)
    {
        var agent = await _dbContext.Agents
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == agentId, cancellationToken);

        return agent == null ? null : MapToResponse(agent);
    }

    public async Task<IReadOnlyList<GetAgentResponseV1>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var agents = await _dbContext.Agents
            .AsNoTracking()
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);

        return agents.Select(MapToResponse).ToList();
    }

    public async Task<GetAgentResponseV1?> UpdateAsync(Guid agentId, CreateAgentRequestV1 request, Guid userId, CancellationToken cancellationToken = default)
    {
        var agent = await _dbContext.Agents
            .FirstOrDefaultAsync(a => a.Id == agentId, cancellationToken);

        if (agent == null)
        {
            return null;
        }

        agent.Name = request.Name;
        agent.Description = request.Description;
        agent.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated agent {AgentId}", agentId);

        return MapToResponse(agent);
    }

    public async Task<bool> DeleteAsync(Guid agentId, Guid userId, CancellationToken cancellationToken = default)
    {
        var agent = await _dbContext.Agents
            .FirstOrDefaultAsync(a => a.Id == agentId, cancellationToken);

        if (agent == null)
        {
            return false;
        }

        _dbContext.Agents.Remove(agent);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted agent {AgentId}", agentId);

        return true;
    }

    private static GetAgentResponseV1 MapToResponse(AgentEntity agent)
    {
        return new GetAgentResponseV1
        {
            Id = agent.Id,
            Name = agent.Name,
            Description = agent.Description,
            CurrentVersionId = agent.CurrentVersionId,
            CreatedAt = agent.CreatedAt,
            UpdatedAt = agent.UpdatedAt
        };
    }
}
