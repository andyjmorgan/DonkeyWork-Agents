using DonkeyWork.Agents.Scheduling.Contracts.Enums;

namespace DonkeyWork.Agents.Persistence.Entities.Scheduling;

public class ScheduledJobEntity : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public ScheduleJobType JobType { get; set; }

    public ScheduleMode ScheduleMode { get; set; }

    public string? CronExpression { get; set; }

    public DateTimeOffset? RunAtUtc { get; set; }

    public string TimeZoneId { get; set; } = "Europe/Dublin";

    public bool IsEnabled { get; set; } = true;

    public bool IsSystem { get; set; }

    public ScheduleTargetType TargetType { get; set; }

    public Guid? TargetAgentDefinitionId { get; set; }

    public Guid? TargetOrchestrationId { get; set; }

    public string QuartzJobKey { get; set; } = string.Empty;

    public string QuartzTriggerKey { get; set; } = string.Empty;

    public string? CreatorEmail { get; set; }

    public string? CreatorName { get; set; }

    public string? CreatorUsername { get; set; }

    public ScheduledJobPayloadEntity? Payload { get; set; }

    public ICollection<ScheduledJobExecutionEntity> Executions { get; set; } = new List<ScheduledJobExecutionEntity>();
}
