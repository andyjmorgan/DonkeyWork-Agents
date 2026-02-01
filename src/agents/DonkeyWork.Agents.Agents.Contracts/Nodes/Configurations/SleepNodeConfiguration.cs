using System.Text.Json.Serialization;
using DonkeyWork.Agents.Agents.Contracts.Nodes.Attributes;
using DonkeyWork.Agents.Agents.Contracts.Nodes.Enums;

namespace DonkeyWork.Agents.Agents.Contracts.Nodes.Configurations;

/// <summary>
/// Configuration for the Sleep node - pauses execution for a specified duration.
/// </summary>
public sealed class SleepNodeConfiguration : NodeConfiguration
{
    /// <inheritdoc />
    public override NodeType NodeType => NodeType.Sleep;

    /// <summary>
    /// Duration to sleep in milliseconds.
    /// </summary>
    [JsonPropertyName("durationMs")]
    [ConfigurableField(Label = "Duration (ms)", ControlType = ControlType.Number, Order = 10, Required = true)]
    public required int DurationMs { get; init; }
}
