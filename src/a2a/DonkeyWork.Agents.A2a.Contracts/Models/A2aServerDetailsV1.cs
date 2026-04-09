using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.A2a.Contracts.Models;

public sealed class A2aServerDetailsV1
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("address")]
    public required string Address { get; init; }

    [JsonPropertyName("isEnabled")]
    public bool IsEnabled { get; init; }

    [JsonPropertyName("connectToNavi")]
    public bool ConnectToNavi { get; init; }

    [JsonPropertyName("publishToMcp")]
    public bool PublishToMcp { get; init; }

    [JsonPropertyName("timeoutSeconds")]
    public int? TimeoutSeconds { get; init; }

    [JsonPropertyName("headerConfigurations")]
    public List<A2aHeaderConfigurationV1> HeaderConfigurations { get; init; } = [];

    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public DateTimeOffset? UpdatedAt { get; init; }
}
