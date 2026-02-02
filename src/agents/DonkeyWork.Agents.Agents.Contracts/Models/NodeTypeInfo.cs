using System.Text.Json.Serialization;
using DonkeyWork.Agents.Agents.Contracts.Nodes.Enums;
using DonkeyWork.Agents.Agents.Contracts.Nodes.Schema;

namespace DonkeyWork.Agents.Agents.Contracts.Models;

/// <summary>
/// Information about a node type and its configuration schema.
/// </summary>
public sealed class NodeTypeInfo
{
    /// <summary>
    /// The node type identifier.
    /// </summary>
    [JsonPropertyName("type")]
    public required NodeType Type { get; init; }

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
    /// Category for grouping in the palette (e.g., "Flow", "AI", "Utility").
    /// </summary>
    [JsonPropertyName("category")]
    public required string Category { get; init; }

    /// <summary>
    /// Icon name for the node (lucide-react icon name).
    /// </summary>
    [JsonPropertyName("icon")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Icon { get; init; }

    /// <summary>
    /// Color theme for the node (e.g., "green", "blue", "purple").
    /// </summary>
    [JsonPropertyName("color")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Color { get; init; }

    /// <summary>
    /// Whether this node has an input handle (can receive connections).
    /// </summary>
    [JsonPropertyName("hasInputHandle")]
    public bool HasInputHandle { get; init; } = true;

    /// <summary>
    /// Whether this node has an output handle (can send connections).
    /// </summary>
    [JsonPropertyName("hasOutputHandle")]
    public bool HasOutputHandle { get; init; } = true;

    /// <summary>
    /// Whether this node can be deleted by the user.
    /// </summary>
    [JsonPropertyName("canDelete")]
    public bool CanDelete { get; init; } = true;

    /// <summary>
    /// Configuration schema for the node.
    /// </summary>
    [JsonPropertyName("configSchema")]
    public required NodeConfigSchema ConfigSchema { get; init; }
}
