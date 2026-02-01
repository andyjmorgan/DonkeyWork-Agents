using System.Text.Json.Serialization;
using DonkeyWork.Agents.Agents.Contracts.Nodes.Attributes;
using DonkeyWork.Agents.Agents.Contracts.Nodes.Enums;
using DonkeyWork.Agents.Agents.Contracts.Nodes.Types;

namespace DonkeyWork.Agents.Agents.Contracts.Nodes.Configurations;

/// <summary>
/// Configuration for the HttpRequest node - makes HTTP requests to external APIs.
/// </summary>
public sealed class HttpRequestNodeConfiguration : NodeConfiguration
{
    /// <inheritdoc />
    public override NodeType NodeType => NodeType.HttpRequest;

    /// <summary>
    /// HTTP method to use.
    /// </summary>
    [JsonPropertyName("method")]
    [ConfigurableField(Label = "Method", ControlType = ControlType.Select, Order = 10, Required = true)]
    public required Enums.HttpMethod Method { get; init; }

    /// <summary>
    /// URL to send the request to. Supports {{variable}} expressions.
    /// </summary>
    [JsonPropertyName("url")]
    [ConfigurableField(Label = "URL", ControlType = ControlType.Text, Order = 20, Required = true)]
    [SupportVariables]
    public required string Url { get; init; }

    /// <summary>
    /// HTTP headers to include in the request.
    /// </summary>
    [JsonPropertyName("headers")]
    [ConfigurableField(Label = "Headers", ControlType = ControlType.KeyValueList, Order = 30)]
    public KeyValueCollection? Headers { get; init; }

    /// <summary>
    /// Request body (for POST, PUT, PATCH). Supports {{variable}} expressions.
    /// </summary>
    [JsonPropertyName("body")]
    [ConfigurableField(Label = "Body", ControlType = ControlType.Code, Order = 40)]
    [SupportVariables]
    public string? Body { get; init; }

    /// <summary>
    /// Request timeout in seconds.
    /// </summary>
    [JsonPropertyName("timeoutSeconds")]
    [ConfigurableField(Label = "Timeout (seconds)", ControlType = ControlType.Slider, Order = 50)]
    [Slider(Min = 1, Max = 300, Step = 1, Default = 30)]
    public int TimeoutSeconds { get; init; } = 30;
}
