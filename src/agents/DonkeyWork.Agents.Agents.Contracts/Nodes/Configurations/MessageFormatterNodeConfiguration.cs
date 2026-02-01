using System.Text.Json.Serialization;
using DonkeyWork.Agents.Agents.Contracts.Nodes.Attributes;
using DonkeyWork.Agents.Agents.Contracts.Nodes.Enums;

namespace DonkeyWork.Agents.Agents.Contracts.Nodes.Configurations;

/// <summary>
/// Configuration for the MessageFormatter node - formats messages using Scriban templates.
/// </summary>
public sealed class MessageFormatterNodeConfiguration : NodeConfiguration
{
    /// <inheritdoc />
    public override NodeType NodeType => NodeType.MessageFormatter;

    /// <summary>
    /// Scriban template for message formatting. Use {{...}} for expressions.
    /// </summary>
    [JsonPropertyName("template")]
    [ConfigurableField(
        Label = "Template",
        ControlType = ControlType.Code,
        Order = 10,
        Required = true,
        Description = "Use {{...}} for Scriban expressions")]
    [SupportVariables]
    public required string Template { get; init; }
}
