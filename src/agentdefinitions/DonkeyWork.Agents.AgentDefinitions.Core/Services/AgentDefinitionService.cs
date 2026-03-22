using System.Text.Json;
using DonkeyWork.Agents.AgentDefinitions.Contracts.Models;
using DonkeyWork.Agents.AgentDefinitions.Contracts.Services;
using DonkeyWork.Agents.Identity.Contracts.Services;
using DonkeyWork.Agents.Persistence;
using DonkeyWork.Agents.Persistence.Entities.AgentDefinitions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.AgentDefinitions.Core.Services;

public class AgentDefinitionService : IAgentDefinitionService
{
    private readonly AgentsDbContext _dbContext;
    private readonly IIdentityContext _identityContext;
    private readonly ILogger<AgentDefinitionService> _logger;

    public AgentDefinitionService(
        AgentsDbContext dbContext,
        IIdentityContext identityContext,
        ILogger<AgentDefinitionService> logger)
    {
        _dbContext = dbContext;
        _identityContext = identityContext;
        _logger = logger;
    }

    public async Task<IReadOnlyList<AgentDefinitionSummaryV1>> ListAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _dbContext.AgentDefinitions
            .AsNoTracking()
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken);

        return entities.Select(MapToSummary).ToList();
    }

    public async Task<AgentDefinitionDetailsV1?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.AgentDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        return entity == null ? null : MapToDetails(entity);
    }

    public async Task<AgentDefinitionDetailsV1> CreateAsync(CreateAgentDefinitionRequestV1 request, CancellationToken cancellationToken = default)
    {
        var defaultContract = JsonDocument.Parse("""
            {
                "lifecycle": "Task",
                "stream": true,
                "maxTokens": 4096,
                "timeoutSeconds": 300
            }
            """);

        var entity = new AgentDefinitionEntity
        {
            Id = Guid.NewGuid(),
            UserId = _identityContext.UserId,
            Name = request.Name,
            Description = request.Description,
            Icon = request.Icon,
            IsSystem = false,
            Contract = defaultContract,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        _dbContext.AgentDefinitions.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created agent definition {Id} for user {UserId}", entity.Id, _identityContext.UserId);

        return MapToDetails(entity);
    }

    public async Task<AgentDefinitionDetailsV1?> UpdateAsync(Guid id, UpdateAgentDefinitionRequestV1 request, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.AgentDefinitions
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (entity == null)
            return null;

        if (request.Name is not null)
            entity.Name = request.Name;

        if (request.Description is not null)
            entity.Description = request.Description;

        if (request.ConnectToNavi is not null)
            entity.ConnectToNavi = request.ConnectToNavi.Value;

        if (request.Icon is not null)
            entity.Icon = request.Icon;

        if (request.Contract is not null)
            entity.Contract = JsonDocument.Parse(request.Contract.Value.GetRawText());

        if (request.ReactFlowData is not null)
            entity.ReactFlowData = JsonDocument.Parse(request.ReactFlowData.Value.GetRawText());

        if (request.NodeConfigurations is not null)
            entity.NodeConfigurations = JsonDocument.Parse(request.NodeConfigurations.Value.GetRawText());

        entity.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated agent definition {Id}", id);

        return MapToDetails(entity);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.AgentDefinitions
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (entity == null)
            return false;

        _dbContext.AgentDefinitions.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted agent definition {Id}", id);

        return true;
    }

    public async Task<IReadOnlyList<NaviAgentDefinitionV1>> GetNaviConnectedAsync(CancellationToken cancellationToken = default)
    {
        var userId = _identityContext.UserId;

        var entities = await _dbContext.AgentDefinitions
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(e => e.ConnectToNavi && e.UserId == userId)
            .ToListAsync(cancellationToken);

        return entities.Select(e => new NaviAgentDefinitionV1
        {
            Id = e.Id,
            Name = e.Name,
            Description = e.Description,
            Icon = e.Icon,
            Contract = e.Contract.RootElement.Clone(),
        }).ToList();
    }

    private static AgentDefinitionSummaryV1 MapToSummary(AgentDefinitionEntity entity)
    {
        return new AgentDefinitionSummaryV1
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            IsSystem = entity.IsSystem,
            ConnectToNavi = entity.ConnectToNavi,
            Icon = entity.Icon,
            CreatedAt = entity.CreatedAt,
        };
    }

    private static AgentDefinitionDetailsV1 MapToDetails(AgentDefinitionEntity entity)
    {
        return new AgentDefinitionDetailsV1
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            IsSystem = entity.IsSystem,
            ConnectToNavi = entity.ConnectToNavi,
            Icon = entity.Icon,
            Contract = entity.Contract.RootElement.Clone(),
            ReactFlowData = entity.ReactFlowData?.RootElement.Clone(),
            NodeConfigurations = entity.NodeConfigurations?.RootElement.Clone(),
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
        };
    }
}
