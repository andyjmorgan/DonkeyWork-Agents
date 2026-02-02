namespace DonkeyWork.Agents.Orchestrations.Contracts.Models.Interfaces;

/// <summary>
/// Container for all interface configurations on an orchestration version.
/// </summary>
public class OrchestrationInterfaces
{
    /// <summary>
    /// MCP (Model Context Protocol) interface configuration.
    /// </summary>
    public McpInterfaceConfig? Mcp { get; set; }

    /// <summary>
    /// A2A (Agent-to-Agent) protocol interface configuration.
    /// </summary>
    public A2aInterfaceConfig? A2a { get; set; }

    /// <summary>
    /// Chat interface configuration.
    /// </summary>
    public ChatInterfaceConfig? Chat { get; set; }

    /// <summary>
    /// Webhook interface configuration.
    /// </summary>
    public WebhookInterfaceConfig? Webhook { get; set; }
}
