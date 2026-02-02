using System.Text.Json.Serialization;
using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Attributes;
using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Enums;

namespace DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Configurations;

/// <summary>
/// Configuration for the Sleep node - pauses execution for a specified duration.
/// </summary>
[Node(
    DisplayName = "Sleep",
    Description = "Pause execution for a specified duration",
    Category = "Utility",
    Icon = "clock",
    Color = "cyan")]
public sealed class SleepNodeConfiguration : NodeConfiguration
{
    /// <inheritdoc />
    public override NodeType NodeType => NodeType.Sleep;

    /// <summary>
    /// Duration to sleep in seconds.
    /// </summary>
    [JsonPropertyName("durationSeconds")]
    [ConfigurableField(Label = "Duration (seconds)", ControlType = ControlType.Number, Order = 10, Required = true, Description = "How long to pause execution")]
    [SupportVariables]
    public required double DurationSeconds { get; init; }
}
