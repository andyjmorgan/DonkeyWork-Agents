using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Agents.Contracts.Models.ReactFlow;

/// <summary>
/// Complete ReactFlow graph data containing nodes, edges, and viewport.
/// </summary>
public sealed class ReactFlowData
{
    [JsonPropertyName("nodes")]
    public List<ReactFlowNode> Nodes { get; set; } = [];

    [JsonPropertyName("edges")]
    public List<ReactFlowEdge> Edges { get; set; } = [];

    [JsonPropertyName("viewport")]
    public ReactFlowViewport Viewport { get; set; } = new();
}
