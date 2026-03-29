using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.A2a.Contracts.Models;

public sealed class A2aAgentCardCapabilitiesV1
{
    [JsonPropertyName("streaming")]
    public bool Streaming { get; init; }

    [JsonPropertyName("pushNotifications")]
    public bool PushNotifications { get; init; }
}
