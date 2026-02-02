using System.Text.Json.Serialization;
using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Enums;

namespace DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Schema;

/// <summary>
/// Schema definition for a node configuration, used by the frontend to render the configuration UI.
/// </summary>
public sealed class NodeConfigSchema
{
    /// <summary>
    /// The type of node this schema describes.
    /// </summary>
    [JsonPropertyName("nodeType")]
    public required NodeType NodeType { get; init; }

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
