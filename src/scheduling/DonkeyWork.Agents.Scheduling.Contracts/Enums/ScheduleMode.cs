using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Scheduling.Contracts.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ScheduleMode
{
    OneOff,
    Recurring
}
