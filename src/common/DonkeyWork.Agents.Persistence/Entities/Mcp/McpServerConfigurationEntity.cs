using DonkeyWork.Agents.Common.Contracts.Enums;

namespace DonkeyWork.Agents.Persistence.Entities.Mcp;

/// <summary>
/// Base configuration entity for MCP server connections.
/// </summary>
public class McpServerConfigurationEntity : BaseEntity
{
    /// <summary>
    /// User-friendly name for the MCP server configuration.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of the MCP server.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Transport type (Stdio or Http).
    /// </summary>
    public McpTransportType TransportType { get; set; }

    /// <summary>
    /// Whether this configuration is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Navigation property to stdio configuration (when TransportType is Stdio).
    /// </summary>
    public McpStdioConfigurationEntity? StdioConfiguration { get; set; }

    /// <summary>
    /// Navigation property to HTTP configuration (when TransportType is Http).
    /// </summary>
    public McpHttpConfigurationEntity? HttpConfiguration { get; set; }
}
