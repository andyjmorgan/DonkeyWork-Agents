using DonkeyWork.Agents.Scheduling.Contracts.Enums;

namespace DonkeyWork.Agents.Scheduling.Contracts.Models;

public class CreateScheduleRequestV1
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public ScheduleJobType JobType { get; set; }

    public ScheduleMode ScheduleMode { get; set; }

    public string? CronExpression { get; set; }

    public DateTimeOffset? RunAtUtc { get; set; }

    public string? TimeZoneId { get; set; }

    public ScheduleTargetType TargetType { get; set; }

    public Guid? TargetAgentDefinitionId { get; set; }

    public Guid? TargetOrchestrationId { get; set; }

    public string UserPrompt { get; set; } = string.Empty;

    public string? InputContext { get; set; }
}
