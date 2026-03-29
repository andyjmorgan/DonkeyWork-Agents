using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.A2a.Contracts.Models;

public sealed class A2aSecuritySchemeV1
{
    [JsonPropertyName("type")]
    public string? Type { get; init; }

    [JsonPropertyName("in")]
    public string? In { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("scheme")]
    public string? Scheme { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }
}
