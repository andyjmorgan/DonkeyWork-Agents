using DonkeyWork.Agents.Common.Contracts.Models.Pagination;
using DonkeyWork.Agents.Identity.Contracts.Services;
using DonkeyWork.Agents.Persistence;
using DonkeyWork.Agents.Persistence.Entities.Scheduling;
using DonkeyWork.Agents.Scheduling.Contracts.Enums;
using DonkeyWork.Agents.Scheduling.Contracts.Models;
using DonkeyWork.Agents.Scheduling.Contracts.Services;
using Microsoft.EntityFrameworkCore;

namespace DonkeyWork.Agents.Scheduling.Core.Services;

public class ScheduledJobRepository : IScheduledJobRepository
{
    private readonly AgentsDbContext _dbContext;
    private readonly IIdentityContext _identityContext;

    public ScheduledJobRepository(AgentsDbContext dbContext, IIdentityContext identityContext)
    {
        _dbContext = dbContext;
        _identityContext = identityContext;
    }

    public async Task<ScheduledJobDetailV1> CreateAsync(
        Guid scheduleId,
        CreateScheduleRequestV1 request,
        string quartzJobKey,
        string quartzTriggerKey,
        CancellationToken ct = default)
    {
        var userId = _identityContext.UserId;
        var now = DateTimeOffset.UtcNow;

        var entity = new ScheduledJobEntity
        {
            Id = scheduleId,
            UserId = userId,
            Name = request.Name,
            Description = request.Description,
            JobType = request.JobType,
            ScheduleMode = request.ScheduleMode,
            CronExpression = request.CronExpression,
            RunAtUtc = request.RunAtUtc,
            TimeZoneId = request.TimeZoneId ?? "Europe/Dublin",
            IsEnabled = true,
            IsSystem = false,
            TargetType = request.TargetType,
            TargetAgentDefinitionId = request.TargetAgentDefinitionId,
            TargetOrchestrationId = request.TargetOrchestrationId,
            QuartzJobKey = quartzJobKey,
            QuartzTriggerKey = quartzTriggerKey,
            CreatorEmail = _identityContext.Email,
            CreatorName = _identityContext.Name,
            CreatorUsername = _identityContext.Username,
            CreatedAt = now,
            UpdatedAt = now,
            Payload = new ScheduledJobPayloadEntity
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                UserPrompt = request.UserPrompt,
                InputContext = request.InputContext,
                Version = 1,
                CreatedAt = now,
                UpdatedAt = now
            }
        };

        _dbContext.ScheduledJobs.Add(entity);
        await _dbContext.SaveChangesAsync(ct);

        return MapToDetail(entity);
    }

    public async Task<ScheduledJobDetailV1?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _dbContext.ScheduledJobs
            .Include(j => j.Payload)
            .AsNoTracking()
            .FirstOrDefaultAsync(j => j.Id == id, ct);

        return entity is null ? null : MapToDetail(entity);
    }

    public async Task<ScheduledJobDetailV1?> GetByQuartzJobKeyAsync(string quartzJobKey, CancellationToken ct = default)
    {
        var entity = await _dbContext.ScheduledJobs
            .Include(j => j.Payload)
            .AsNoTracking()
            .FirstOrDefaultAsync(j => j.QuartzJobKey == quartzJobKey, ct);

        return entity is null ? null : MapToDetail(entity);
    }

    public async Task<PaginatedResponse<ScheduledJobSummaryV1>> ListAsync(
        ScheduleJobType? jobType = null,
        ScheduleTargetType? targetType = null,
        ScheduleMode? scheduleMode = null,
        bool? isEnabled = null,
        bool includeSystem = false,
        PaginationRequest? pagination = null,
        CancellationToken ct = default)
    {
        var query = _dbContext.ScheduledJobs.AsNoTracking().AsQueryable();

        if (!includeSystem)
            query = query.Where(j => !j.IsSystem);

        if (jobType.HasValue)
            query = query.Where(j => j.JobType == jobType.Value);

        if (targetType.HasValue)
            query = query.Where(j => j.TargetType == targetType.Value);

        if (scheduleMode.HasValue)
            query = query.Where(j => j.ScheduleMode == scheduleMode.Value);

        if (isEnabled.HasValue)
            query = query.Where(j => j.IsEnabled == isEnabled.Value);

        var totalCount = await query.CountAsync(ct);

        var offset = pagination?.Offset ?? 0;
        var limit = pagination?.Limit ?? 50;

        var entities = await query
            .OrderByDescending(j => j.CreatedAt)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(ct);

        var items = entities.Select(MapToSummary).ToList();

        return new PaginatedResponse<ScheduledJobSummaryV1>
        {
            Items = items,
            TotalCount = totalCount,
            Offset = offset,
            Limit = limit
        };
    }

    public async Task<ScheduledJobDetailV1?> UpdateAsync(Guid id, UpdateScheduleRequestV1 request, CancellationToken ct = default)
    {
        var entity = await _dbContext.ScheduledJobs
            .Include(j => j.Payload)
            .FirstOrDefaultAsync(j => j.Id == id, ct);

        if (entity is null)
            return null;

        if (request.Name is not null) entity.Name = request.Name;
        if (request.Description is not null) entity.Description = request.Description;
        if (request.JobType.HasValue) entity.JobType = request.JobType.Value;
        if (request.CronExpression is not null) entity.CronExpression = request.CronExpression;
        if (request.RunAtUtc.HasValue) entity.RunAtUtc = request.RunAtUtc;
        if (request.TimeZoneId is not null) entity.TimeZoneId = request.TimeZoneId;
        if (request.TargetType.HasValue) entity.TargetType = request.TargetType.Value;
        if (request.TargetAgentDefinitionId.HasValue) entity.TargetAgentDefinitionId = request.TargetAgentDefinitionId;
        if (request.TargetOrchestrationId.HasValue) entity.TargetOrchestrationId = request.TargetOrchestrationId;
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        if (entity.Payload is not null)
        {
            if (request.UserPrompt is not null) entity.Payload.UserPrompt = request.UserPrompt;
            if (request.InputContext is not null) entity.Payload.InputContext = request.InputContext;
            entity.Payload.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await _dbContext.SaveChangesAsync(ct);

        return MapToDetail(entity);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _dbContext.ScheduledJobs.FirstOrDefaultAsync(j => j.Id == id, ct);
        if (entity is null)
            return false;

        _dbContext.ScheduledJobs.Remove(entity);
        await _dbContext.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> SetEnabledAsync(Guid id, bool enabled, CancellationToken ct = default)
    {
        var entity = await _dbContext.ScheduledJobs.FirstOrDefaultAsync(j => j.Id == id, ct);
        if (entity is null)
            return false;

        entity.IsEnabled = enabled;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(ct);
        return true;
    }

    private static ScheduledJobDetailV1 MapToDetail(ScheduledJobEntity entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Description = entity.Description,
        JobType = entity.JobType,
        ScheduleMode = entity.ScheduleMode,
        CronExpression = entity.CronExpression,
        RunAtUtc = entity.RunAtUtc,
        TimeZoneId = entity.TimeZoneId,
        IsEnabled = entity.IsEnabled,
        TargetType = entity.TargetType,
        TargetAgentDefinitionId = entity.TargetAgentDefinitionId,
        TargetOrchestrationId = entity.TargetOrchestrationId,
        QuartzJobKey = entity.QuartzJobKey,
        QuartzTriggerKey = entity.QuartzTriggerKey,
        Payload = entity.Payload is null ? null : new ScheduledJobPayloadV1
        {
            UserPrompt = entity.Payload.UserPrompt,
            InputContext = entity.Payload.InputContext,
            Version = entity.Payload.Version
        },
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };

    private static ScheduledJobSummaryV1 MapToSummary(ScheduledJobEntity entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        JobType = entity.JobType,
        ScheduleMode = entity.ScheduleMode,
        CronExpression = entity.CronExpression,
        RunAtUtc = entity.RunAtUtc,
        TimeZoneId = entity.TimeZoneId,
        IsEnabled = entity.IsEnabled,
        TargetType = entity.TargetType,
        CreatedAt = entity.CreatedAt
    };
}
