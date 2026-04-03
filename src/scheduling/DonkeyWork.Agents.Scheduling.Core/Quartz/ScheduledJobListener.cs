using DonkeyWork.Agents.Persistence;
using DonkeyWork.Agents.Persistence.Entities.Scheduling;
using DonkeyWork.Agents.Scheduling.Contracts.Enums;
using DonkeyWork.Agents.Scheduling.Contracts.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;

namespace DonkeyWork.Agents.Scheduling.Core.Quartz;

public class ScheduledJobListener : IJobListener
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ScheduledJobListener> _logger;

    public ScheduledJobListener(IServiceScopeFactory scopeFactory, ILogger<ScheduledJobListener> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public string Name => "ScheduledJobListener";

    public async Task JobToBeExecuted(IJobExecutionContext context, CancellationToken ct = default)
    {
        try
        {
            var scheduleIdStr = context.MergedJobDataMap.GetString(ScheduleDataMapKeys.ScheduleId);
            if (!Guid.TryParse(scheduleIdStr, out var scheduleId))
                return;

            await using var scope = _scopeFactory.CreateAsyncScope();
            var executionRepo = scope.ServiceProvider.GetRequiredService<IScheduledJobExecutionRepository>();
            var dbContext = scope.ServiceProvider.GetRequiredService<AgentsDbContext>();

            var job = await dbContext.ScheduledJobs
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(j => j.Id == scheduleId, ct);

            if (job is null)
                return;

            var triggerSource = job.ScheduleMode == ScheduleMode.Recurring
                ? ScheduleTriggerSource.Cron
                : ScheduleTriggerSource.OneOff;

            if (context.Trigger is ISimpleTrigger && context.Trigger.Key.Name.Contains("manual", StringComparison.OrdinalIgnoreCase))
                triggerSource = ScheduleTriggerSource.Manual;

            var executionId = await executionRepo.CreateAsync(
                scheduleId,
                job.UserId,
                triggerSource,
                context.FireInstanceId,
                context.Scheduler.SchedulerInstanceId,
                ct);

            context.Put(ScheduleDataMapKeys.ScheduleExecutionId, executionId.ToString());

            _logger.LogInformation("Created execution {ExecutionId} for schedule {ScheduleId}", executionId, scheduleId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create execution record in JobToBeExecuted");
        }
    }

    public Task JobExecutionVetoed(IJobExecutionContext context, CancellationToken ct = default)
    {
        return UpdateExecutionStatus(context, ScheduleExecutionStatus.Cancelled, null, ct);
    }

    public async Task JobWasExecuted(IJobExecutionContext context, JobExecutionException? jobException, CancellationToken ct = default)
    {
        var status = jobException is null
            ? ScheduleExecutionStatus.Succeeded
            : ScheduleExecutionStatus.Failed;

        var errorDetails = jobException?.InnerException?.Message ?? jobException?.Message;

        await UpdateExecutionStatus(context, status, errorDetails, ct);

        if (status == ScheduleExecutionStatus.Succeeded)
        {
            await DisableOneOffIfApplicable(context, ct);
        }
    }

    private async Task UpdateExecutionStatus(
        IJobExecutionContext context,
        ScheduleExecutionStatus status,
        string? errorDetails,
        CancellationToken ct)
    {
        try
        {
            var executionIdStr = context.Get(ScheduleDataMapKeys.ScheduleExecutionId) as string;
            if (!Guid.TryParse(executionIdStr, out var executionId))
                return;

            await using var scope = _scopeFactory.CreateAsyncScope();
            var executionRepo = scope.ServiceProvider.GetRequiredService<IScheduledJobExecutionRepository>();

            await executionRepo.UpdateCompletionAsync(executionId, status, errorDetails, null, null, ct);

            _logger.LogInformation("Updated execution {ExecutionId} to {Status}", executionId, status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update execution record in listener");
        }
    }

    private async Task DisableOneOffIfApplicable(IJobExecutionContext context, CancellationToken ct)
    {
        try
        {
            var scheduleIdStr = context.MergedJobDataMap.GetString(ScheduleDataMapKeys.ScheduleId);
            if (!Guid.TryParse(scheduleIdStr, out var scheduleId))
                return;

            await using var scope = _scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AgentsDbContext>();

            var job = await dbContext.ScheduledJobs
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(j => j.Id == scheduleId, ct);

            if (job is null || job.ScheduleMode != ScheduleMode.OneOff)
                return;

            job.IsEnabled = false;
            job.UpdatedAt = DateTimeOffset.UtcNow;
            await dbContext.SaveChangesAsync(ct);

            _logger.LogInformation("Disabled one-off schedule {ScheduleId} after successful execution", scheduleId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to disable one-off schedule in listener");
        }
    }
}
