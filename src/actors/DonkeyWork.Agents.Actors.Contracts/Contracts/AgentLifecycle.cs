using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Actors.Contracts.Contracts;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AgentLifecycle
{
    Task,
    Linger,
}
