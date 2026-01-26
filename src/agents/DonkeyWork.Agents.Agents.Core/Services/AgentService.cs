using System.Text.Json;
using DonkeyWork.Agents.Agents.Contracts.Models;
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
        var startNodeId = Guid.NewGuid().ToString();
        var endNodeId = Guid.NewGuid().ToString();

        // Create default template: Start -> End
        var defaultReactFlowData = new
        {
            nodes = new[]
            {
                new
                {
                    id = startNodeId,
                    type = "start",
                    position = new { x = 250, y = 50 },
                    data = new { label = "start" }
                },
                new
                {
                    id = endNodeId,
                    type = "end",
                    position = new { x = 250, y = 250 },
                    data = new { label = "end" }
                }
            },
            edges = new[]
            {
                new
                {
                    id = Guid.NewGuid().ToString(),
                    source = startNodeId,
                    target = endNodeId,
                    type = "smoothstep",
                    animated = true
                }
            },
            viewport = new { x = 0, y = 0, zoom = 1 }
        };

        var defaultNodeConfigurations = new Dictionary<string, object>
        {
            [startNodeId] = new { type = "start", name = "start_1" },
            [endNodeId] = new { type = "end", name = "end_1" }
        };

        var defaultInputSchema = new
        {
            type = "object",
            properties = new
            {
                input = new { type = "string" }
            },
            required = new[] { "input" }
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
            InputSchema = JsonSerializer.Serialize(defaultInputSchema),
            ReactFlowData = JsonSerializer.Serialize(defaultReactFlowData),
            NodeConfigurations = JsonSerializer.Serialize(defaultNodeConfigurations),
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
