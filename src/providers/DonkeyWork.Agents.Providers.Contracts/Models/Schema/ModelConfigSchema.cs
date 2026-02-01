using System.Text.Json.Serialization;
using DonkeyWork.Agents.Common.Contracts.Enums;
using DonkeyWork.Agents.Common.Sdk.Models.Schema;
using DonkeyWork.Agents.Providers.Contracts.Enums;

namespace DonkeyWork.Agents.Providers.Contracts.Models.Schema;

/// <summary>
/// Complete configuration schema for a specific model.
/// </summary>
public sealed class ModelConfigSchema
{
    [JsonPropertyName("modelId")]
    public required string ModelId { get; init; }

    [JsonPropertyName("modelName")]
    public required string ModelName { get; init; }

    [JsonPropertyName("provider")]
    public required LlmProvider Provider { get; init; }

    [JsonPropertyName("mode")]
    public required ModelMode Mode { get; init; }

    [JsonPropertyName("tabs")]
    public IReadOnlyList<TabSchema> Tabs { get; init; } = [];

    [JsonPropertyName("fields")]
    public required IReadOnlyList<ConfigFieldSchema> Fields { get; init; }
}
