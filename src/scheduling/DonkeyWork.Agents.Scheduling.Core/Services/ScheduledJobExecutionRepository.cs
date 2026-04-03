using DonkeyWork.Agents.Common.Contracts.Models.Pagination;
using DonkeyWork.Agents.Persistence;
using DonkeyWork.Agents.Persistence.Entities.Scheduling;
using DonkeyWork.Agents.Scheduling.Contracts.Enums;
using DonkeyWork.Agents.Scheduling.Contracts.Models;
using DonkeyWork.Agents.Scheduling.Contracts.Services;
using Microsoft.EntityFrameworkCore;

namespace DonkeyWork.Agents.Scheduling.Core.Services;

public class ScheduledJobExecutionRepository : IScheduledJobExecutionRepository
{
    private readonly AgentsDbContext _dbContext;

    public ScheduledJobExecutionRepository(AgentsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Guid> CreateAsync(
        Guid scheduledJobId,
        Guid userId,
        ScheduleTriggerSource triggerSource,
        string? quartzFireInstanceId,
        string? executingNodeId,
        CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;

        var entity = new ScheduledJobExecutionEntity
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ScheduledJobId = scheduledJobId,
            QuartzFireInstanceId = quartzFireInstanceId,
            TriggerSource = triggerSource,
            StartedAtUtc = now,
            Status = ScheduleExecutionStatus.Running,
            ExecutingNodeId = executingNodeId,
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.ScheduledJobExecutions.Add(entity);
        await _dbContext.SaveChangesAsync(ct);
        return entity.Id;
    }

    public async Task UpdateCompletionAsync(
        Guid id,
        ScheduleExecutionStatus status,
        string? errorDetails,
        string? outputSummary,
        Guid? correlationId,
        CancellationToken ct = default)
    {
        var entity = await _dbContext.ScheduledJobExecutions
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.Id == id, ct);

        if (entity is null)
            return;

        entity.Status = status;
        entity.CompletedAtUtc = DateTimeOffset.UtcNow;
        entity.ErrorDetails = errorDetails;
        entity.OutputSummary = outputSummary;
        entity.CorrelationId = correlationId;
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task<ScheduledJobExecutionV1?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _dbContext.ScheduledJobExecutions
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, ct);

        return entity is null ? null : MapToModel(entity);
    }

    public async Task<PaginatedResponse<ScheduledJobExecutionV1>> ListByScheduleIdAsync(
        Guid scheduledJobId,
        PaginationRequest? pagination = null,
        CancellationToken ct = default)
    {
        var query = _dbContext.ScheduledJobExecutions
            .AsNoTracking()
            .Where(e => e.ScheduledJobId == scheduledJobId);

        var totalCount = await query.CountAsync(ct);

        var offset = pagination?.Offset ?? 0;
        var limit = pagination?.Limit ?? 50;

        var entities = await query
            .OrderByDescending(e => e.StartedAtUtc)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(ct);

        return new PaginatedResponse<ScheduledJobExecutionV1>
        {
            Items = entities.Select(MapToModel).ToList(),
            TotalCount = totalCount,
            Offset = offset,
            Limit = limit
        };
    }

    private static ScheduledJobExecutionV1 MapToModel(ScheduledJobExecutionEntity entity) => new()
    {
        Id = entity.Id,
        ScheduledJobId = entity.ScheduledJobId,
        QuartzFireInstanceId = entity.QuartzFireInstanceId,
        TriggerSource = entity.TriggerSource,
        StartedAtUtc = entity.StartedAtUtc,
        CompletedAtUtc = entity.CompletedAtUtc,
        Status = entity.Status,
        ErrorDetails = entity.ErrorDetails,
        OutputSummary = entity.OutputSummary,
        ExecutingNodeId = entity.ExecutingNodeId,
        CorrelationId = entity.CorrelationId
    };
}
