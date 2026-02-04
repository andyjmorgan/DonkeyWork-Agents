using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Orchestrations.Contracts.Models.Interfaces;

/// <summary>
/// Configuration for Webhook interface.
/// </summary>
public class WebhookInterfaceConfig : InterfaceConfig
{
    /// <summary>
    /// HTTP methods allowed for this webhook.
    /// </summary>
    [JsonPropertyName("allowedMethods")]
    public List<string> AllowedMethods { get; set; } = ["POST"];

    /// <summary>
    /// Whether to validate webhook signatures.
    /// </summary>
    [JsonPropertyName("requireSignature")]
    public bool RequireSignature { get; set; }
}
