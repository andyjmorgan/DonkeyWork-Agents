using System.Text.Json.Serialization;
using DonkeyWork.Agents.Common.Contracts.Enums;

namespace DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Schema;

/// <summary>
/// Schema definition for multimodal chat model configuration.
/// </summary>
public sealed class MultimodalChatSchema
{
    /// <summary>
    /// The LLM provider this schema is for.
    /// </summary>
    [JsonPropertyName("provider")]
    public required LlmProvider Provider { get; init; }

    /// <summary>
    /// Display name for the provider (e.g., "OpenAI", "Anthropic", "Google").
    /// </summary>
    [JsonPropertyName("providerDisplayName")]
    public required string ProviderDisplayName { get; init; }

    /// <summary>
    /// The tabs available for organizing fields.
    /// </summary>
    [JsonPropertyName("tabs")]
    public required IReadOnlyList<TabSchema> Tabs { get; init; }

    /// <summary>
    /// The fields available for configuration.
    /// </summary>
    [JsonPropertyName("fields")]
    public required IReadOnlyList<FieldSchema> Fields { get; init; }
}
