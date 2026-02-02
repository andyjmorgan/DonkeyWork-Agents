using System.Text.Json.Serialization;
using DonkeyWork.Agents.Agents.Contracts.Nodes.Enums;

namespace DonkeyWork.Agents.Agents.Contracts.Models.ReactFlow;

/// <summary>
/// A node in the ReactFlow graph.
/// </summary>
public sealed class ReactFlowNode
{
    [JsonPropertyName("id")]
    public required Guid Id { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = "schemaNode";

    [JsonPropertyName("position")]
    public required ReactFlowPosition Position { get; set; }

    [JsonPropertyName("data")]
    public required ReactFlowNodeData Data { get; set; }

    [JsonPropertyName("selected")]
    public bool Selected { get; set; }
}

/// <summary>
/// Position of a node in the ReactFlow canvas.
/// </summary>
public sealed class ReactFlowPosition
{
    [JsonPropertyName("x")]
    public double X { get; set; }

    [JsonPropertyName("y")]
    public double Y { get; set; }
}

/// <summary>
/// Data payload for a ReactFlow node.
/// </summary>
public sealed class ReactFlowNodeData
{
    [JsonPropertyName("nodeType")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required NodeType NodeType { get; set; }

    [JsonPropertyName("label")]
    public required string Label { get; set; }

    [JsonPropertyName("displayName")]
    public required string DisplayName { get; set; }

    [JsonPropertyName("icon")]
    public string? Icon { get; set; }

    [JsonPropertyName("color")]
    public string? Color { get; set; }

    [JsonPropertyName("hasInputHandle")]
    public bool HasInputHandle { get; set; } = true;

    [JsonPropertyName("hasOutputHandle")]
    public bool HasOutputHandle { get; set; } = true;

    [JsonPropertyName("canDelete")]
    public bool CanDelete { get; set; } = true;
}
