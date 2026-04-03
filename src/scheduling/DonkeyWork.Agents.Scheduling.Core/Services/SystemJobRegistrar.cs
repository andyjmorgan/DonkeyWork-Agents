using DonkeyWork.Agents.Persistence;
using DonkeyWork.Agents.Persistence.Entities.Scheduling;
using DonkeyWork.Agents.Scheduling.Contracts.Enums;
using DonkeyWork.Agents.Scheduling.Contracts.Models;
using DonkeyWork.Agents.Scheduling.Core.Quartz;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz;

namespace DonkeyWork.Agents.Scheduling.Core.Services;

public class SystemJobRegistrar : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ISchedulerFactory _schedulerFactory;
    private readonly IEnumerable<SystemJobDefinition> _definitions;
    private readonly ILogger<SystemJobRegistrar> _logger;

    public SystemJobRegistrar(
        IServiceScopeFactory scopeFactory,
        ISchedulerFactory schedulerFactory,
        IEnumerable<SystemJobDefinition> definitions,
        ILogger<SystemJobRegistrar> logger)
    {
        _scopeFactory = scopeFactory;
        _schedulerFactory = schedulerFactory;
        _definitions = definitions;
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

            await RegisterAllAsync(scheduler, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Shutdown
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "System job registration failed");
        }
    }

    private async Task RegisterAllAsync(IScheduler scheduler, CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AgentsDbContext>();

        var registered = 0;
        var updated = 0;

        foreach (var definition in _definitions)
        {
            try
            {
                var existing = await dbContext.ScheduledJobs
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(j => j.IsSystem && j.Name == definition.Name, ct);

                if (existing is null)
                {
                    await CreateSystemJob(dbContext, scheduler, definition, ct);
                    registered++;
                }
                else if (existing.CronExpression != definition.CronExpression)
                {
                    existing.CronExpression = definition.CronExpression;
                    existing.UpdatedAt = DateTimeOffset.UtcNow;
                    await dbContext.SaveChangesAsync(ct);

                    var triggerKey = QuartzKeyFactory.CreateTriggerKey(existing.Id, isSystem: true);
                    var jobKey = QuartzKeyFactory.CreateJobKey(existing.Id, isSystem: true);

                    var newTrigger = TriggerBuilder.Create()
                        .WithIdentity(triggerKey)
                        .ForJob(jobKey)
                        .WithCronSchedule(definition.CronExpression, x => x
                            .WithMisfireHandlingInstructionFireAndProceed())
                        .Build();

                    await scheduler.RescheduleJob(triggerKey, newTrigger, ct);
                    updated++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register system job {Name}", definition.Name);
            }
        }

        _logger.LogInformation("System job registration complete: {Registered} registered, {Updated} updated, {Total} total",
            registered, updated, _definitions.Count());
    }

    private async Task CreateSystemJob(
        AgentsDbContext dbContext,
        IScheduler scheduler,
        SystemJobDefinition definition,
        CancellationToken ct)
    {
        var scheduleId = Guid.NewGuid();
        var jobKey = QuartzKeyFactory.CreateJobKey(scheduleId, isSystem: true);
        var triggerKey = QuartzKeyFactory.CreateTriggerKey(scheduleId, isSystem: true);
        var now = DateTimeOffset.UtcNow;

        var entity = new ScheduledJobEntity
        {
            Id = scheduleId,
            UserId = Guid.Empty,
            Name = definition.Name,
            Description = $"System maintenance job: {definition.Name}",
            JobType = definition.JobType,
            ScheduleMode = ScheduleMode.Recurring,
            CronExpression = definition.CronExpression,
            TimeZoneId = "UTC",
            IsEnabled = true,
            IsSystem = true,
            TargetType = ScheduleTargetType.Navi,
            QuartzJobKey = QuartzKeyFactory.FormatJobKey(jobKey),
            QuartzTriggerKey = QuartzKeyFactory.FormatTriggerKey(triggerKey),
            CreatedAt = now,
            UpdatedAt = now,
        };

        dbContext.ScheduledJobs.Add(entity);
        await dbContext.SaveChangesAsync(ct);

        var job = JobBuilder.Create<SystemTaskJob>()
            .WithIdentity(jobKey)
            .UsingJobData(ScheduleDataMapKeys.ScheduleId, scheduleId.ToString())
            .UsingJobData(ScheduleDataMapKeys.SystemHandlerType, definition.HandlerType.AssemblyQualifiedName!)
            .StoreDurably()
            .RequestRecovery()
            .Build();

        var trigger = TriggerBuilder.Create()
            .WithIdentity(triggerKey)
            .ForJob(jobKey)
            .WithCronSchedule(definition.CronExpression, x => x
                .WithMisfireHandlingInstructionFireAndProceed())
            .Build();

        await scheduler.ScheduleJob(job, trigger, ct);

        _logger.LogInformation("Registered system job {Name} ({ScheduleId}) with cron {Cron}",
            definition.Name, scheduleId, definition.CronExpression);
    }
}
