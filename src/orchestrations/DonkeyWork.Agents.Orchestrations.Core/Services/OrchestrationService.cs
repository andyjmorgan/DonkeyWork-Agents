using System.Text.Json;
using DonkeyWork.Agents.Orchestrations.Contracts.Models;
using DonkeyWork.Agents.Orchestrations.Contracts.Models.ReactFlow;
using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Configurations;
using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Enums;
using DonkeyWork.Agents.Orchestrations.Contracts.Services;
using DonkeyWork.Agents.Persistence;
using DonkeyWork.Agents.Persistence.Entities.Orchestrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Orchestrations.Core.Services;

public class OrchestrationService : IOrchestrationService
{
    private readonly AgentsDbContext _dbContext;
    private readonly ILogger<OrchestrationService> _logger;

    public OrchestrationService(AgentsDbContext dbContext, ILogger<OrchestrationService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<CreateOrchestrationResponseV1> CreateAsync(CreateOrchestrationRequestV1 request, Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating agent for user {UserId} with name {Name}", userId, request.Name);

        var agentId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var startNodeId = Guid.NewGuid();
        var endNodeId = Guid.NewGuid();

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

        var agent = new OrchestrationEntity
        {
            Id = agentId,
            UserId = userId,
            Name = request.Name,
            Description = request.Description,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var version = new OrchestrationVersionEntity
        {
            Id = versionId,
            UserId = userId,
            OrchestrationId = agentId,
            VersionNumber = 1,
            IsDraft = true,
            InputSchema = defaultInputSchema,
            ReactFlowData = defaultReactFlowData,
            NodeConfigurations = defaultNodeConfigurations,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.Orchestrations.Add(agent);
        _dbContext.OrchestrationVersions.Add(version);

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created agent {AgentId} with initial draft version {VersionId}", agentId, versionId);

        return new CreateOrchestrationResponseV1
        {
            Id = agentId,
            Name = agent.Name,
            Description = agent.Description,
            VersionId = versionId,
            CreatedAt = agent.CreatedAt
        };
    }

    public async Task<GetOrchestrationResponseV1?> GetByIdAsync(Guid agentId, Guid userId, CancellationToken cancellationToken = default)
    {
        var agent = await _dbContext.Orchestrations
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == agentId, cancellationToken);

        return agent == null ? null : MapToResponse(agent);
    }

    public async Task<IReadOnlyList<GetOrchestrationResponseV1>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var agents = await _dbContext.Orchestrations
            .AsNoTracking()
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);

        return agents.Select(MapToResponse).ToList();
    }

    public async Task<GetOrchestrationResponseV1?> UpdateAsync(Guid agentId, CreateOrchestrationRequestV1 request, Guid userId, CancellationToken cancellationToken = default)
    {
        var agent = await _dbContext.Orchestrations
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
        var agent = await _dbContext.Orchestrations
            .FirstOrDefaultAsync(a => a.Id == agentId, cancellationToken);

        if (agent == null)
        {
            return false;
        }

        _dbContext.Orchestrations.Remove(agent);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted agent {AgentId}", agentId);

        return true;
    }

    public async Task<IReadOnlyList<ChatEnabledOrchestrationV1>> ListChatEnabledAsync(CancellationToken cancellationToken = default)
    {
        var orchestrations = await _dbContext.Orchestrations
            .AsNoTracking()
            .Include(o => o.CurrentVersion)
            .Where(o => o.CurrentVersionId != null)
            .ToListAsync(cancellationToken);

        return orchestrations
            .Where(o => o.CurrentVersion?.NaviEnabled == true)
            .Select(o => new ChatEnabledOrchestrationV1
            {
                Id = o.Id,
                Name = o.Name,
                Description = o.Description
            })
            .ToList();
    }

    public async Task<IReadOnlyList<ToolEnabledOrchestrationV1>> ListToolEnabledAsync(CancellationToken cancellationToken = default)
    {
        var orchestrations = await _dbContext.Orchestrations
            .AsNoTracking()
            .Include(o => o.CurrentVersion)
            .Where(o => o.CurrentVersionId != null)
            .ToListAsync(cancellationToken);

        var registry = Orchestrations.Contracts.Nodes.Registry.NodeConfigurationRegistry.Instance;

        return orchestrations
            .Where(o => o.CurrentVersion?.ToolEnabled == true)
            .Select(o => new ToolEnabledOrchestrationV1
            {
                Orchestration = MapToResponse(o),
                Version = new GetOrchestrationVersionResponseV1
                {
                    Id = o.CurrentVersion!.Id,
                    OrchestrationId = o.Id,
                    VersionNumber = o.CurrentVersion.VersionNumber,
                    IsDraft = o.CurrentVersion.IsDraft,
                    InputSchema = o.CurrentVersion.InputSchema.RootElement.Clone(),
                    OutputSchema = o.CurrentVersion.OutputSchema?.RootElement.Clone(),
                    ReactFlowData = o.CurrentVersion.ReactFlowData,
                    NodeConfigurations = System.Text.Json.JsonSerializer.SerializeToElement(
                        o.CurrentVersion.NodeConfigurations, registry.JsonOptions),
                    DirectEnabled = o.CurrentVersion.DirectEnabled,
                    ToolEnabled = o.CurrentVersion.ToolEnabled,
                    McpEnabled = o.CurrentVersion.McpEnabled,
                    NaviEnabled = o.CurrentVersion.NaviEnabled,
                    CreatedAt = o.CurrentVersion.CreatedAt,
                    PublishedAt = o.CurrentVersion.PublishedAt
                }
            })
            .ToList();
    }

    private static GetOrchestrationResponseV1 MapToResponse(OrchestrationEntity agent)
    {
        return new GetOrchestrationResponseV1
        {
            Id = agent.Id,
            Name = agent.Name,
            Description = agent.Description,
            FriendlyName = agent.FriendlyName,
            CurrentVersionId = agent.CurrentVersionId,
            CreatedAt = agent.CreatedAt,
            UpdatedAt = agent.UpdatedAt
        };
    }
}
