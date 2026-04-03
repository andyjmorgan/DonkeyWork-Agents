using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Scheduling.Contracts.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ScheduleTriggerSource
{
    Cron,
    OneOff,
    Manual,
    Webhook
}
