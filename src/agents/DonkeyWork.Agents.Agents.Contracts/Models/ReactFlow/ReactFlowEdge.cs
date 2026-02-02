using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Agents.Contracts.Models.ReactFlow;

/// <summary>
/// An edge connecting two nodes in the ReactFlow graph.
/// </summary>
public sealed class ReactFlowEdge
{
    [JsonPropertyName("id")]
    public required Guid Id { get; set; }

    [JsonPropertyName("source")]
    public required Guid Source { get; set; }

    [JsonPropertyName("target")]
    public required Guid Target { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = "smoothstep";

    [JsonPropertyName("animated")]
    public bool Animated { get; set; } = true;
}
