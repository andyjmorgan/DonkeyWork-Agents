using System.Text.Json.Serialization;
using DonkeyWork.Agents.Common.Nodes.Attributes;
using DonkeyWork.Agents.Common.Nodes.Enums;

namespace DonkeyWork.Agents.Common.Nodes.Configurations;

/// <summary>
/// Configuration for the Sleep node - pauses execution for a specified duration.
/// </summary>
public sealed class SleepNodeConfiguration : NodeConfiguration
{
    /// <inheritdoc />
    public override NodeType Type => NodeType.Sleep;

    /// <summary>
    /// Duration to sleep in milliseconds.
    /// </summary>
    [JsonPropertyName("durationMs")]
    [ConfigurableField(Label = "Duration (ms)", ControlType = ControlType.Number, Order = 10, Required = true)]
    public required int DurationMs { get; init; }
}
