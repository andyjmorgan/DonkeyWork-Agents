using DonkeyWork.Agents.Scheduling.Contracts.Enums;

namespace DonkeyWork.Agents.Scheduling.Contracts.Models;

public class UpdateScheduleRequestV1
{
    public string? Name { get; set; }

    public string? Description { get; set; }

    public ScheduleJobType? JobType { get; set; }

    public string? CronExpression { get; set; }

    public DateTimeOffset? RunAtUtc { get; set; }

    public string? TimeZoneId { get; set; }

    public ScheduleTargetType? TargetType { get; set; }

    public Guid? TargetAgentDefinitionId { get; set; }

    public Guid? TargetOrchestrationId { get; set; }

    public string? UserPrompt { get; set; }

    public string? InputContext { get; set; }
}
