using DonkeyWork.Agents.Identity.Contracts.Services;
using DonkeyWork.Agents.Persistence;
using DonkeyWork.Agents.Persistence.Entities.Prompts;
using DonkeyWork.Agents.Prompts.Contracts.Enums;
using DonkeyWork.Agents.Prompts.Contracts.Models;
using DonkeyWork.Agents.Prompts.Contracts.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Prompts.Core.Services;

public class PromptService : IPromptService
{
    private readonly AgentsDbContext _dbContext;
    private readonly IIdentityContext _identityContext;
    private readonly ILogger<PromptService> _logger;

    public PromptService(
        AgentsDbContext dbContext,
        IIdentityContext identityContext,
        ILogger<PromptService> logger)
    {
        _dbContext = dbContext;
        _identityContext = identityContext;
        _logger = logger;
    }

    public async Task<IReadOnlyList<PromptSummaryV1>> ListAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _dbContext.Prompts
            .AsNoTracking()
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken);

        return entities.Select(MapToSummary).ToList();
    }

    public async Task<PromptDetailsV1?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Prompts
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        return entity == null ? null : MapToDetails(entity);
    }

    public async Task<PromptDetailsV1> CreateAsync(CreatePromptRequestV1 request, CancellationToken cancellationToken = default)
    {
        var entity = new PromptEntity
        {
            Id = Guid.NewGuid(),
            UserId = _identityContext.UserId,
            Name = request.Name,
            Description = request.Description,
            Content = request.Content,
            PromptType = request.PromptType.ToString(),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        _dbContext.Prompts.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created prompt {Id} for user {UserId}", entity.Id, _identityContext.UserId);

        return MapToDetails(entity);
    }

    public async Task<PromptDetailsV1?> UpdateAsync(Guid id, UpdatePromptRequestV1 request, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Prompts
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (entity == null)
            return null;

        if (request.Name is not null)
            entity.Name = request.Name;

        if (request.Description is not null)
            entity.Description = request.Description;

        if (request.Content is not null)
            entity.Content = request.Content;

        if (request.PromptType is not null)
            entity.PromptType = request.PromptType.Value.ToString();

        entity.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated prompt {Id}", id);

        return MapToDetails(entity);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Prompts
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (entity == null)
            return false;

        _dbContext.Prompts.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted prompt {Id}", id);

        return true;
    }

    private static PromptSummaryV1 MapToSummary(PromptEntity entity)
    {
        return new PromptSummaryV1
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            PromptType = Enum.Parse<PromptType>(entity.PromptType),
            CreatedAt = entity.CreatedAt,
        };
    }

    private static PromptDetailsV1 MapToDetails(PromptEntity entity)
    {
        return new PromptDetailsV1
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            Content = entity.Content,
            PromptType = Enum.Parse<PromptType>(entity.PromptType),
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
        };
    }
}
