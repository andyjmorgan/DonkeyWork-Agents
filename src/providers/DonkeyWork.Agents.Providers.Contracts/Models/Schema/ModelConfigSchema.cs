using System.Text.Json.Serialization;
using DonkeyWork.Agents.Common.Contracts.Enums;
using DonkeyWork.Agents.Providers.Contracts.Enums;

namespace DonkeyWork.Agents.Providers.Contracts.Models.Schema;

/// <summary>
/// Complete configuration schema for a specific model.
/// </summary>
public sealed class ModelConfigSchema
{
    [JsonPropertyName("model_id")]
    public required string ModelId { get; init; }

    [JsonPropertyName("model_name")]
    public required string ModelName { get; init; }

    [JsonPropertyName("provider")]
    public required LlmProvider Provider { get; init; }

    [JsonPropertyName("mode")]
    public required ModelMode Mode { get; init; }

    [JsonPropertyName("fields")]
    public required IReadOnlyList<ConfigFieldSchema> Fields { get; init; }
}
