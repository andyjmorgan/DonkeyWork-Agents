using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Agents.Contracts.Models.NodeConfigurations;

/// <summary>
/// Configuration for a Message Formatter node that renders Scriban templates.
/// </summary>
public sealed class MessageFormatterNodeConfiguration : NodeConfiguration
{
    /// <summary>
    /// The Scriban template to render.
    /// Supports variables like {{input.property}}, {{steps.nodeName.property}}, etc.
    /// </summary>
    [JsonPropertyName("template")]
    public required string Template { get; init; }
}
