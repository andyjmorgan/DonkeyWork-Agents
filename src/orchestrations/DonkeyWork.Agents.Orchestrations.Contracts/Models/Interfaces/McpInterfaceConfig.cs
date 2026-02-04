using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Orchestrations.Contracts.Models.Interfaces;

/// <summary>
/// Configuration for MCP (Model Context Protocol) interface.
/// </summary>
public class McpInterfaceConfig : InterfaceConfig
{
    /// <summary>
    /// Tool name exposed via MCP.
    /// </summary>
    [JsonPropertyName("toolName")]
    public string? ToolName { get; set; }
}
