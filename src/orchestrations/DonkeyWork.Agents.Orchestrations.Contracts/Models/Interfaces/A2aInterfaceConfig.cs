using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Orchestrations.Contracts.Models.Interfaces;

/// <summary>
/// Configuration for A2A (Agent-to-Agent) protocol interface.
/// </summary>
public class A2aInterfaceConfig : InterfaceConfig
{
    /// <summary>
    /// Unique agent identifier for A2A discovery.
    /// </summary>
    [JsonPropertyName("agentId")]
    public string? AgentId { get; set; }

    /// <summary>
    /// Capabilities this orchestration advertises.
    /// </summary>
    [JsonPropertyName("capabilities")]
    public List<string> Capabilities { get; set; } = [];
}
