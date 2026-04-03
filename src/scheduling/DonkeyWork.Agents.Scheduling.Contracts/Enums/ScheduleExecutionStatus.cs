using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Scheduling.Contracts.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ScheduleExecutionStatus
{
    Running,
    Succeeded,
    Failed,
    Cancelled
}
