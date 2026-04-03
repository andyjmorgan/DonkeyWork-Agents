using DonkeyWork.Agents.Scheduling.Contracts.Enums;

namespace DonkeyWork.Agents.Scheduling.Contracts.Models;

public sealed class SystemJobDefinition
{
    public required string Name { get; init; }

    public required string CronExpression { get; init; }

    public required ScheduleJobType JobType { get; init; }

    public required Type HandlerType { get; init; }
}
