using System.Text.Json;
using System.Text.Json.Serialization;
using DonkeyWork.Agents.Common.Nodes.Attributes;
using DonkeyWork.Agents.Common.Nodes.Enums;

namespace DonkeyWork.Agents.Common.Nodes.Configurations;

/// <summary>
/// Configuration for the End node - the exit point of an agent workflow.
/// </summary>
public sealed class EndNodeConfiguration : NodeConfiguration
{
    /// <inheritdoc />
    public override NodeType Type => NodeType.End;

    /// <summary>
    /// JSON Schema defining the output format (optional - for validation).
    /// </summary>
    [JsonPropertyName("outputSchema")]
    [ConfigurableField(Label = "Output Schema", ControlType = ControlType.Json, Order = 10)]
    public JsonElement? OutputSchema { get; init; }
}
