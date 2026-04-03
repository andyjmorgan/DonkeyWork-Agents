using DonkeyWork.Agents.Common.Contracts.Models.Pagination;
using DonkeyWork.Agents.Scheduling.Contracts.Enums;
using DonkeyWork.Agents.Scheduling.Contracts.Models;
using DonkeyWork.Agents.Scheduling.Contracts.Services;
using DonkeyWork.Agents.Scheduling.Core.Quartz;
using Microsoft.Extensions.Logging;
using Quartz;

namespace DonkeyWork.Agents.Scheduling.Core.Services;

public class SchedulingService : ISchedulingService
{
    private readonly IScheduledJobRepository _jobRepository;
    private readonly IScheduledJobExecutionRepository _executionRepository;
    private readonly ISchedulerFactory _schedulerFactory;
    private readonly int _minimumCronIntervalHours;
    private readonly string _defaultTimeZone;
    private readonly ILogger<SchedulingService> _logger;

    public SchedulingService(
        IScheduledJobRepository jobRepository,
        IScheduledJobExecutionRepository executionRepository,
        ISchedulerFactory schedulerFactory,
        SchedulingServiceOptions options,
        ILogger<SchedulingService> logger)
    {
        _jobRepository = jobRepository;
        _executionRepository = executionRepository;
        _schedulerFactory = schedulerFactory;
        _minimumCronIntervalHours = options.MinimumCronIntervalHours;
        _defaultTimeZone = options.DefaultTimeZone;
        _logger = logger;
    }

    public async Task<CreateScheduleResponseV1> CreateAsync(CreateScheduleRequestV1 request, CancellationToken ct = default)
    {
        request.TimeZoneId ??= _defaultTimeZone;

        if (request.ScheduleMode == ScheduleMode.Recurring)
        {
            ValidateCronExpression(request.CronExpression!);
        }

        var scheduleId = Guid.NewGuid();
        var jobKey = QuartzKeyFactory.CreateJobKey(scheduleId);
        var triggerKey = QuartzKeyFactory.CreateTriggerKey(scheduleId);

        var detail = await _jobRepository.CreateAsync(
            request,
            QuartzKeyFactory.FormatJobKey(jobKey),
            QuartzKeyFactory.FormatTriggerKey(triggerKey),
            ct);

        var scheduler = await _schedulerFactory.GetScheduler(ct);

        var job = BuildJobDetail(detail.Id, jobKey);
        var trigger = BuildTrigger(request, triggerKey, jobKey, request.TimeZoneId);

        await scheduler.ScheduleJob(job, trigger, ct);

        var nextFire = (await scheduler.GetTrigger(triggerKey, ct))?.GetNextFireTimeUtc();

        _logger.LogInformation("Created schedule {ScheduleId} ({Name}) with next fire at {NextFire}",
            detail.Id, detail.Name, nextFire);

        return new CreateScheduleResponseV1
        {
            Id = detail.Id,
            Name = detail.Name,
            NextFireTimeUtc = nextFire,
            CreatedAt = detail.CreatedAt
        };
    }

    public async Task<ScheduledJobDetailV1?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var detail = await _jobRepository.GetAsync(id, ct);
        if (detail is null)
            return null;

        var scheduler = await _schedulerFactory.GetScheduler(ct);
        await EnrichWithFireTimes(detail, scheduler, ct);
        return detail;
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
        var result = await _jobRepository.ListAsync(jobType, targetType, scheduleMode, isEnabled, includeSystem, pagination, ct);

        var scheduler = await _schedulerFactory.GetScheduler(ct);
        foreach (var item in result.Items)
        {
            await EnrichSummaryWithFireTimes(item, scheduler, ct);
        }

        return result;
    }

    public async Task<UpdateScheduleResponseV1?> UpdateAsync(Guid id, UpdateScheduleRequestV1 request, CancellationToken ct = default)
    {
        if (request.CronExpression is not null)
        {
            ValidateCronExpression(request.CronExpression);
        }

        var existing = await _jobRepository.GetAsync(id, ct);
        if (existing is null)
            return null;

        var updated = await _jobRepository.UpdateAsync(id, request, ct);
        if (updated is null)
            return null;

        var scheduleChanged = request.CronExpression is not null || request.RunAtUtc.HasValue || request.TimeZoneId is not null;
        if (scheduleChanged)
        {
            await RescheduleQuartzTrigger(updated, ct);
        }

        var scheduler = await _schedulerFactory.GetScheduler(ct);
        var triggerKey = ParseTriggerKey(updated.QuartzTriggerKey);
        var nextFire = triggerKey is not null
            ? (await scheduler.GetTrigger(triggerKey, ct))?.GetNextFireTimeUtc()
            : null;

        return new UpdateScheduleResponseV1
        {
            Id = updated.Id,
            Name = updated.Name,
            NextFireTimeUtc = nextFire,
            UpdatedAt = updated.UpdatedAt
        };
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var detail = await _jobRepository.GetAsync(id, ct);
        if (detail is null)
            return false;

        var scheduler = await _schedulerFactory.GetScheduler(ct);
        var jobKey = ParseJobKey(detail.QuartzJobKey);
        if (jobKey is not null)
        {
            await scheduler.DeleteJob(jobKey, ct);
        }

        return await _jobRepository.DeleteAsync(id, ct);
    }

    public async Task<bool> EnableAsync(Guid id, CancellationToken ct = default)
    {
        var detail = await _jobRepository.GetAsync(id, ct);
        if (detail is null)
            return false;

        var result = await _jobRepository.SetEnabledAsync(id, true, ct);
        if (!result)
            return false;

        var scheduler = await _schedulerFactory.GetScheduler(ct);
        var jobKey = ParseJobKey(detail.QuartzJobKey);
        if (jobKey is not null)
        {
            await scheduler.ResumeJob(jobKey, ct);
        }

        return true;
    }

    public async Task<bool> DisableAsync(Guid id, CancellationToken ct = default)
    {
        var detail = await _jobRepository.GetAsync(id, ct);
        if (detail is null)
            return false;

        var result = await _jobRepository.SetEnabledAsync(id, false, ct);
        if (!result)
            return false;

        var scheduler = await _schedulerFactory.GetScheduler(ct);
        var jobKey = ParseJobKey(detail.QuartzJobKey);
        if (jobKey is not null)
        {
            await scheduler.PauseJob(jobKey, ct);
        }

        return true;
    }

    public async Task<bool> TriggerAsync(Guid id, CancellationToken ct = default)
    {
        var detail = await _jobRepository.GetAsync(id, ct);
        if (detail is null)
            return false;

        var scheduler = await _schedulerFactory.GetScheduler(ct);
        var jobKey = ParseJobKey(detail.QuartzJobKey);
        if (jobKey is null)
            return false;

        await scheduler.TriggerJob(jobKey, ct);

        _logger.LogInformation("Manually triggered schedule {ScheduleId} ({Name})", id, detail.Name);
        return true;
    }

    public Task<PaginatedResponse<ScheduledJobExecutionV1>> ListExecutionsAsync(
        Guid scheduleId, PaginationRequest? pagination = null, CancellationToken ct = default)
        => _executionRepository.ListByScheduleIdAsync(scheduleId, pagination, ct);

    public Task<ScheduledJobExecutionV1?> GetExecutionAsync(Guid executionId, CancellationToken ct = default)
        => _executionRepository.GetAsync(executionId, ct);

    private void ValidateCronExpression(string cronExpression)
    {
        var normalized = CronHelper.NormalizeToQuartzCron(cronExpression);

        if (!CronHelper.IsValid(normalized))
            throw new ArgumentException($"Invalid cron expression: {cronExpression}");

        if (!CronHelper.MeetsMinimumInterval(normalized, _minimumCronIntervalHours))
            throw new ArgumentException($"Cron interval must be at least {_minimumCronIntervalHours} hours.");
    }

    private static IJobDetail BuildJobDetail(Guid scheduleId, JobKey jobKey)
    {
        return JobBuilder.Create<ScheduledTaskJob>()
            .WithIdentity(jobKey)
            .UsingJobData(ScheduleDataMapKeys.ScheduleId, scheduleId.ToString())
            .StoreDurably()
            .RequestRecovery()
            .Build();
    }

    private static ITrigger BuildTrigger(
        CreateScheduleRequestV1 request,
        TriggerKey triggerKey,
        JobKey jobKey,
        string timeZoneId)
    {
        var builder = TriggerBuilder.Create()
            .WithIdentity(triggerKey)
            .ForJob(jobKey);

        if (request.ScheduleMode == ScheduleMode.Recurring)
        {
            var normalized = CronHelper.NormalizeToQuartzCron(request.CronExpression!);
            var tz = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);

            builder.WithCronSchedule(normalized, x => x
                .InTimeZone(tz)
                .WithMisfireHandlingInstructionFireAndProceed());
        }
        else
        {
            if (request.RunAtUtc.HasValue)
            {
                builder.StartAt(request.RunAtUtc.Value);
            }
            else
            {
                builder.StartNow();
            }
        }

        return builder.Build();
    }

    private async Task RescheduleQuartzTrigger(ScheduledJobDetailV1 detail, CancellationToken ct)
    {
        var scheduler = await _schedulerFactory.GetScheduler(ct);
        var triggerKey = ParseTriggerKey(detail.QuartzTriggerKey);
        if (triggerKey is null)
            return;

        var jobKey = ParseJobKey(detail.QuartzJobKey);
        if (jobKey is null)
            return;

        var newTrigger = TriggerBuilder.Create()
            .WithIdentity(triggerKey)
            .ForJob(jobKey);

        if (detail.ScheduleMode == ScheduleMode.Recurring && detail.CronExpression is not null)
        {
            var normalized = CronHelper.NormalizeToQuartzCron(detail.CronExpression);
            var tz = TimeZoneInfo.FindSystemTimeZoneById(detail.TimeZoneId);

            newTrigger.WithCronSchedule(normalized, x => x
                .InTimeZone(tz)
                .WithMisfireHandlingInstructionFireAndProceed());
        }
        else if (detail.RunAtUtc.HasValue)
        {
            newTrigger.StartAt(detail.RunAtUtc.Value);
        }

        await scheduler.RescheduleJob(triggerKey, newTrigger.Build(), ct);
    }

    private async Task EnrichWithFireTimes(ScheduledJobDetailV1 detail, IScheduler scheduler, CancellationToken ct)
    {
        var triggerKey = ParseTriggerKey(detail.QuartzTriggerKey);
        if (triggerKey is null)
            return;

        var trigger = await scheduler.GetTrigger(triggerKey, ct);
        if (trigger is null)
            return;

        detail.NextFireTimeUtc = trigger.GetNextFireTimeUtc();
        detail.PrevFireTimeUtc = trigger.GetPreviousFireTimeUtc();
    }

    private async Task EnrichSummaryWithFireTimes(ScheduledJobSummaryV1 summary, IScheduler scheduler, CancellationToken ct)
    {
        // Summary doesn't carry QuartzTriggerKey directly, so we reconstruct from the schedule ID
        var triggerKey = QuartzKeyFactory.CreateTriggerKey(summary.Id);
        var trigger = await scheduler.GetTrigger(triggerKey, ct);
        if (trigger is null)
            return;

        summary.NextFireTimeUtc = trigger.GetNextFireTimeUtc();
        summary.PrevFireTimeUtc = trigger.GetPreviousFireTimeUtc();
    }

    private static JobKey? ParseJobKey(string? formatted)
    {
        if (string.IsNullOrEmpty(formatted))
            return null;

        var dotIndex = formatted.IndexOf('.');
        if (dotIndex < 0)
            return new JobKey(formatted);

        return new JobKey(formatted[(dotIndex + 1)..], formatted[..dotIndex]);
    }

    private static TriggerKey? ParseTriggerKey(string? formatted)
    {
        if (string.IsNullOrEmpty(formatted))
            return null;

        var dotIndex = formatted.IndexOf('.');
        if (dotIndex < 0)
            return new TriggerKey(formatted);

        return new TriggerKey(formatted[(dotIndex + 1)..], formatted[..dotIndex]);
    }
}
