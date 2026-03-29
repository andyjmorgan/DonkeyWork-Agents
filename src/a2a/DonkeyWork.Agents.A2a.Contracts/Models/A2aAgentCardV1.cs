using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.A2a.Contracts.Models;

public sealed class A2aAgentCardV1
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("url")]
    public string? Url { get; init; }

    [JsonPropertyName("version")]
    public string? Version { get; init; }

    [JsonPropertyName("skills")]
    public List<A2aAgentCardSkillV1>? Skills { get; init; }

    [JsonPropertyName("capabilities")]
    public A2aAgentCardCapabilitiesV1? Capabilities { get; init; }

    [JsonPropertyName("defaultInputModes")]
    public List<string>? DefaultInputModes { get; init; }

    [JsonPropertyName("defaultOutputModes")]
    public List<string>? DefaultOutputModes { get; init; }
}
