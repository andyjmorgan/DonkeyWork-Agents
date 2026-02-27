using DonkeyWork.Agents.Conversations.Contracts.Services;
using DonkeyWork.Agents.Persistence;
using DonkeyWork.Agents.Persistence.Entities.Conversations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Conversations.Core.Services;

public class ConversationMetadataService : IConversationMetadataService
{
    private readonly AgentsDbContext _dbContext;
    private readonly ILogger<ConversationMetadataService> _logger;

    public ConversationMetadataService(AgentsDbContext dbContext, ILogger<ConversationMetadataService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task EnsureExistsAsync(Guid conversationId, Guid userId, string title, CancellationToken ct = default)
    {
        var exists = await _dbContext.Conversations
            .IgnoreQueryFilters()
            .AnyAsync(c => c.Id == conversationId, ct);

        if (exists)
            return;

        var now = DateTimeOffset.UtcNow;
        var entity = new ConversationEntity
        {
            Id = conversationId,
            UserId = userId,
            OrchestrationId = null,
            Title = title,
            CreatedAt = now,
            UpdatedAt = now,
        };

        _dbContext.Conversations.Add(entity);
        await _dbContext.SaveChangesAsync(ct);
        _logger.LogInformation("Created conversation metadata record {ConversationId} for user {UserId}", conversationId, userId);
    }

    public async Task UpdateTitleAsync(Guid conversationId, Guid userId, string title, CancellationToken ct = default)
    {
        var entity = await _dbContext.Conversations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == conversationId && c.UserId == userId, ct);

        if (entity is null)
            return;

        entity.Title = title;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task TouchTimestampAsync(Guid conversationId, Guid userId, CancellationToken ct = default)
    {
        var entity = await _dbContext.Conversations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == conversationId && c.UserId == userId, ct);

        if (entity is null)
            return;

        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(ct);
    }
}
