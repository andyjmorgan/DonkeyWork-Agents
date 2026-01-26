using System.Text.Json;
using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Agents.Contracts.Models;

/// <summary>
/// Information about a node type and its configuration schema.
/// </summary>
public sealed class NodeTypeInfo
{
    /// <summary>
    /// The node type identifier (e.g., "start", "model", "end").
    /// </summary>
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    /// <summary>
    /// Display name for the node type.
    /// </summary>
    [JsonPropertyName("displayName")]
    public required string DisplayName { get; init; }

    /// <summary>
    /// Description of what the node does.
    /// </summary>
    [JsonPropertyName("description")]
    public required string Description { get; init; }

    /// <summary>
    /// JSON Schema for the node's configuration.
    /// </summary>
    [JsonPropertyName("configSchema")]
    public required JsonElement ConfigSchema { get; init; }
}
