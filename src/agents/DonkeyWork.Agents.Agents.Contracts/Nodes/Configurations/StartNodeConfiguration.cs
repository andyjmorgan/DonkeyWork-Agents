using System.Text.Json;
using System.Text.Json.Serialization;
using DonkeyWork.Agents.Agents.Contracts.Nodes.Attributes;
using DonkeyWork.Agents.Agents.Contracts.Nodes.Enums;

namespace DonkeyWork.Agents.Agents.Contracts.Nodes.Configurations;

/// <summary>
/// Configuration for the Start node - the entry point of an agent workflow.
/// </summary>
public sealed class StartNodeConfiguration : NodeConfiguration
{
    /// <inheritdoc />
    public override NodeType NodeType => NodeType.Start;

    /// <summary>
    /// JSON Schema defining the expected input format for the agent.
    /// </summary>
    [JsonPropertyName("inputSchema")]
    [ConfigurableField(Label = "Input Schema", ControlType = ControlType.Json, Order = 10)]
    public required JsonElement InputSchema { get; init; }
}
