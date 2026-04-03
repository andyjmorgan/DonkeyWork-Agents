using DonkeyWork.Agents.Persistence;
using DonkeyWork.Agents.Persistence.Entities.Scheduling;
using DonkeyWork.Agents.Scheduling.Core.Quartz;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz;

namespace DonkeyWork.Agents.Scheduling.Core.Services;

public class ScheduleReconciliationService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ISchedulerFactory _schedulerFactory;
    private readonly ILogger<ScheduleReconciliationService> _logger;

    public ScheduleReconciliationService(
        IServiceScopeFactory scopeFactory,
        ISchedulerFactory schedulerFactory,
        ILogger<ScheduleReconciliationService> logger)
    {
        _scopeFactory = scopeFactory;
        _schedulerFactory = schedulerFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var scheduler = await _schedulerFactory.GetScheduler(stoppingToken);

            while (!scheduler.IsStarted && !stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(500, stoppingToken);
            }

            if (stoppingToken.IsCancellationRequested)
                return;

            await ReconcileAsync(scheduler, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Shutdown
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Schedule reconciliation failed");
        }
    }

    private async Task ReconcileAsync(IScheduler scheduler, CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AgentsDbContext>();

        var schedules = await dbContext.ScheduledJobs
            .IgnoreQueryFilters()
            .Where(j => !j.IsSystem)
            .ToListAsync(ct);

        var recreated = 0;
        var paused = 0;

        foreach (var schedule in schedules)
        {
            var jobKey = QuartzKeyFactory.CreateJobKey(schedule.Id);

            if (!await scheduler.CheckExists(jobKey, ct))
            {
                if (!schedule.IsEnabled)
                    continue;

                await RecreateQuartzJob(scheduler, schedule, jobKey, ct);
                recreated++;
            }
            else if (!schedule.IsEnabled)
            {
                var state = await scheduler.GetTriggerState(
                    QuartzKeyFactory.CreateTriggerKey(schedule.Id), ct);

                if (state != TriggerState.Paused)
                {
                    await scheduler.PauseJob(jobKey, ct);
                    paused++;
                }
            }
        }

        _logger.LogInformation(
            "Schedule reconciliation complete: {Recreated} recreated, {Paused} paused, {Total} total schedules",
            recreated, paused, schedules.Count);
    }

    private async Task RecreateQuartzJob(IScheduler scheduler, ScheduledJobEntity schedule, JobKey jobKey, CancellationToken ct)
    {
        try
        {
            var triggerKey = QuartzKeyFactory.CreateTriggerKey(schedule.Id);

            var job = JobBuilder.Create<ScheduledTaskJob>()
                .WithIdentity(jobKey)
                .UsingJobData(ScheduleDataMapKeys.ScheduleId, schedule.Id.ToString())
                .StoreDurably()
                .RequestRecovery()
                .Build();

            var triggerBuilder = TriggerBuilder.Create()
                .WithIdentity(triggerKey)
                .ForJob(jobKey);

            if (schedule.ScheduleMode == Contracts.Enums.ScheduleMode.Recurring && schedule.CronExpression is not null)
            {
                var normalized = CronHelper.NormalizeToQuartzCron(schedule.CronExpression);
                var tz = TimeZoneInfo.FindSystemTimeZoneById(schedule.TimeZoneId);
                triggerBuilder.WithCronSchedule(normalized, x => x
                    .InTimeZone(tz)
                    .WithMisfireHandlingInstructionFireAndProceed());
            }
            else if (schedule.RunAtUtc.HasValue)
            {
                triggerBuilder.StartAt(schedule.RunAtUtc.Value);
            }
            else
            {
                _logger.LogWarning("Schedule {ScheduleId} has no cron expression or run-at time, skipping", schedule.Id);
                return;
            }

            await scheduler.ScheduleJob(job, triggerBuilder.Build(), ct);

            _logger.LogInformation("Recreated Quartz job for schedule {ScheduleId} ({Name})", schedule.Id, schedule.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to recreate Quartz job for schedule {ScheduleId}", schedule.Id);
        }
    }
}
