namespace DonkeyWork.Agents.Orchestrations.Contracts.Models.Interfaces;

/// <summary>
/// Configuration for Webhook interface.
/// </summary>
public class WebhookInterfaceConfig : InterfaceConfig
{
    /// <summary>
    /// HTTP methods allowed for this webhook.
    /// </summary>
    public List<string> AllowedMethods { get; set; } = new() { "POST" };

    /// <summary>
    /// Whether to validate webhook signatures.
    /// </summary>
    public bool RequireSignature { get; set; }
}
