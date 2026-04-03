using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Scheduling.Contracts.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ScheduleTargetType
{
    Navi,
    CustomAgent,
    Orchestration
}
