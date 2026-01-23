using System.Text.Json.Serialization;
using DonkeyWork.Agents.Common.Contracts.Enums;
using DonkeyWork.Agents.Providers.Contracts.Enums;

namespace DonkeyWork.Agents.Providers.Contracts.Models;

public sealed class ModelDefinition
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("provider")]
    public required LlmProvider Provider { get; init; }

    [JsonPropertyName("mode")]
    public required ModelMode Mode { get; init; }

    [JsonPropertyName("max_input_tokens")]
    public required int MaxInputTokens { get; init; }

    [JsonPropertyName("max_output_tokens")]
    public required int MaxOutputTokens { get; init; }

    [JsonPropertyName("input_cost_per_million_tokens")]
    public required decimal InputCostPerMillionTokens { get; init; }

    [JsonPropertyName("output_cost_per_million_tokens")]
    public required decimal OutputCostPerMillionTokens { get; init; }

    [JsonPropertyName("supports")]
    public required ModelSupports Supports { get; init; }

    [JsonPropertyName("client_types")]
    public required IReadOnlyList<ProviderClientType> ClientTypes { get; init; }
}
