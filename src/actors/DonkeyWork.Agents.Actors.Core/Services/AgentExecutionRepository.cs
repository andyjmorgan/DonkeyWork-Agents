using DonkeyWork.Agents.Actors.Contracts.Models;
using DonkeyWork.Agents.Actors.Contracts.Services;
using DonkeyWork.Agents.Persistence;
using DonkeyWork.Agents.Persistence.Entities.Actors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Actors.Core.Services;

/// <summary>
/// EF Core implementation of agent execution persistence.
/// Uses IDbContextFactory for short-lived DbContext instances (same as GrainMessageStore).
/// All writes are wrapped in try/catch to never fail agent execution.
/// </summary>
public sealed class AgentExecutionRepository : IAgentExecutionRepository
{
    private readonly IDbContextFactory<AgentsDbContext> _dbContextFactory;
    private readonly ILogger<AgentExecutionRepository> _logger;

    public AgentExecutionRepository(
        IDbContextFactory<AgentsDbContext> dbContextFactory,
        ILogger<AgentExecutionRepository> logger)
    {
        _dbContextFactory = dbContextFactory;
        _logger = logger;
    }

    public async Task<Guid> CreateAsync(
        Guid userId,
        Guid conversationId,
        string agentType,
        string label,
        string grainKey,
        string? parentGrainKey,
        string contractSnapshot,
        string? input,
        string? modelId,
        CancellationToken ct = default)
    {
        try
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            var entity = new AgentExecutionEntity
            {
                UserId = userId,
                ConversationId = conversationId,
                AgentType = agentType,
                Label = label,
                GrainKey = grainKey,
                ParentGrainKey = parentGrainKey,
                ContractSnapshot = contractSnapshot,
                Input = input,
                Status = "Running",
                StartedAt = DateTimeOffset.UtcNow,
                ModelId = modelId,
            };

            dbContext.AgentExecutions.Add(entity);
            await dbContext.SaveChangesAsync(ct);

            _logger.LogDebug(
                "Created execution {ExecutionId} for {AgentType} '{Label}' grain={GrainKey}",
                entity.Id, agentType, label, grainKey);

            return entity.Id;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to create execution record for {AgentType} '{Label}' grain={GrainKey}",
                agentType, label, grainKey);
            return Guid.Empty;
        }
    }

    public async Task UpdateCompletionAsync(
        Guid executionId,
        Guid userId,
        string status,
        string? output,
        string? errorMessage,
        long? durationMs,
        int? inputTokens,
        int? outputTokens,
        CancellationToken ct = default)
    {
        if (executionId == Guid.Empty) return;

        try
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            var entity = await dbContext.AgentExecutions
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(e => e.Id == executionId && e.UserId == userId, ct);

            if (entity is null)
            {
                _logger.LogWarning("Execution {ExecutionId} not found for completion update", executionId);
                return;
            }

            entity.Status = status;
            entity.Output = output;
            entity.ErrorMessage = errorMessage;
            entity.CompletedAt = DateTimeOffset.UtcNow;
            entity.DurationMs = durationMs;
            entity.InputTokensUsed = inputTokens;
            entity.OutputTokensUsed = outputTokens;
            entity.UpdatedAt = DateTimeOffset.UtcNow;

            await dbContext.SaveChangesAsync(ct);

            _logger.LogDebug(
                "Updated execution {ExecutionId} status={Status} duration={DurationMs}ms",
                executionId, status, durationMs);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update completion for execution {ExecutionId}", executionId);
        }
    }

    public async Task UpdateTokensAsync(
        Guid executionId,
        Guid userId,
        int inputTokens,
        int outputTokens,
        CancellationToken ct = default)
    {
        if (executionId == Guid.Empty) return;

        try
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            var entity = await dbContext.AgentExecutions
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(e => e.Id == executionId && e.UserId == userId, ct);

            if (entity is null) return;

            entity.InputTokensUsed = inputTokens;
            entity.OutputTokensUsed = outputTokens;
            entity.UpdatedAt = DateTimeOffset.UtcNow;

            await dbContext.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update tokens for execution {ExecutionId}", executionId);
        }
    }

    public async Task<AgentExecutionDetailV1?> GetByIdAsync(Guid executionId, Guid userId, CancellationToken ct = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

        var entity = await dbContext.AgentExecutions
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == executionId && e.UserId == userId, ct);

        return entity is null ? null : MapToDetail(entity);
    }

    public async Task<AgentExecutionDetailV1?> GetByGrainKeyAsync(string grainKey, Guid userId, CancellationToken ct = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

        var entity = await dbContext.AgentExecutions
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.GrainKey == grainKey && e.UserId == userId, ct);

        return entity is null ? null : MapToDetail(entity);
    }

    public async Task<IReadOnlyList<AgentExecutionSummaryV1>> ListByConversationAsync(
        Guid conversationId, Guid userId, CancellationToken ct = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

        var entities = await dbContext.AgentExecutions
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(e => e.ConversationId == conversationId && e.UserId == userId)
            .OrderBy(e => e.StartedAt)
            .ToListAsync(ct);

        return entities.Select(MapToSummary).ToList();
    }

    public async Task<(IReadOnlyList<AgentExecutionSummaryV1> Items, int TotalCount)> ListAsync(
        Guid userId, int offset, int limit, CancellationToken ct = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

        var query = dbContext.AgentExecutions
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(e => e.UserId == userId);

        var totalCount = await query.CountAsync(ct);

        var entities = await query
            .OrderByDescending(e => e.StartedAt)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(ct);

        return (entities.Select(MapToSummary).ToList(), totalCount);
    }

    private static AgentExecutionDetailV1 MapToDetail(AgentExecutionEntity entity) => new()
    {
        Id = entity.Id,
        ConversationId = entity.ConversationId,
        AgentType = entity.AgentType,
        Label = entity.Label,
        GrainKey = entity.GrainKey,
        ParentGrainKey = entity.ParentGrainKey,
        ContractSnapshot = entity.ContractSnapshot,
        Input = entity.Input,
        Output = entity.Output,
        Status = entity.Status,
        ErrorMessage = entity.ErrorMessage,
        ModelId = entity.ModelId,
        StartedAt = entity.StartedAt,
        CompletedAt = entity.CompletedAt,
        DurationMs = entity.DurationMs,
        InputTokensUsed = entity.InputTokensUsed,
        OutputTokensUsed = entity.OutputTokensUsed,
    };

    private static AgentExecutionSummaryV1 MapToSummary(AgentExecutionEntity entity) => new()
    {
        Id = entity.Id,
        ConversationId = entity.ConversationId,
        AgentType = entity.AgentType,
        Label = entity.Label,
        GrainKey = entity.GrainKey,
        Status = entity.Status,
        ModelId = entity.ModelId,
        StartedAt = entity.StartedAt,
        CompletedAt = entity.CompletedAt,
        DurationMs = entity.DurationMs,
        InputTokensUsed = entity.InputTokensUsed,
        OutputTokensUsed = entity.OutputTokensUsed,
    };
}
