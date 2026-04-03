using DonkeyWork.Agents.Scheduling.Contracts.Enums;

namespace DonkeyWork.Agents.Scheduling.Contracts.Models;

public class ScheduledJobExecutionV1
{
    public Guid Id { get; set; }

    public Guid ScheduledJobId { get; set; }

    public string? QuartzFireInstanceId { get; set; }

    public ScheduleTriggerSource TriggerSource { get; set; }

    public DateTimeOffset StartedAtUtc { get; set; }

    public DateTimeOffset? CompletedAtUtc { get; set; }

    public ScheduleExecutionStatus Status { get; set; }

    public string? ErrorDetails { get; set; }

    public string? OutputSummary { get; set; }

    public string? ExecutingNodeId { get; set; }

    public Guid? CorrelationId { get; set; }
}
