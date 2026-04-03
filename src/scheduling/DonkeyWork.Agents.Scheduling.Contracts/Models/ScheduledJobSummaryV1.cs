using DonkeyWork.Agents.Scheduling.Contracts.Enums;

namespace DonkeyWork.Agents.Scheduling.Contracts.Models;

public class ScheduledJobSummaryV1
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public ScheduleJobType JobType { get; set; }

    public ScheduleMode ScheduleMode { get; set; }

    public string? CronExpression { get; set; }

    public DateTimeOffset? RunAtUtc { get; set; }

    public string TimeZoneId { get; set; } = string.Empty;

    public bool IsEnabled { get; set; }

    public ScheduleTargetType TargetType { get; set; }

    public string? TargetName { get; set; }

    public DateTimeOffset? NextFireTimeUtc { get; set; }

    public DateTimeOffset? PrevFireTimeUtc { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
