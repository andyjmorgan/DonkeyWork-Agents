using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Orchestrations.Contracts.Models.ReactFlow;

/// <summary>
/// Viewport state of the ReactFlow canvas.
/// </summary>
public sealed class ReactFlowViewport
{
    [JsonPropertyName("x")]
    public double X { get; set; }

    [JsonPropertyName("y")]
    public double Y { get; set; }

    [JsonPropertyName("zoom")]
    public double Zoom { get; set; } = 1;
}
