using System.Text.Json;
using System.Text.Json.Serialization;
using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Attributes;
using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Enums;

namespace DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Configurations;

/// <summary>
/// Configuration for the End node - the exit point of an orchestration workflow.
/// </summary>
[Node(
    DisplayName = "End",
    Description = "Output and completion",
    Category = "Flow",
    Icon = "flag",
    Color = "orange",
    HasOutputHandle = false,
    CanDelete = false)]
public sealed class EndNodeConfiguration : NodeConfiguration
{
    /// <inheritdoc />
    public override NodeType NodeType => NodeType.End;

    /// <summary>
    /// JSON Schema defining the output format (optional - for validation).
    /// </summary>
    [JsonPropertyName("outputSchema")]
    [ConfigurableField(Label = "Output Schema", ControlType = ControlType.Json, Order = 10)]
    public JsonElement? OutputSchema { get; init; }
}
