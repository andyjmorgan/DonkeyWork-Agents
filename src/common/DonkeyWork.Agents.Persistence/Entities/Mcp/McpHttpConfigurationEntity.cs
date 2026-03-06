using DonkeyWork.Agents.Common.Contracts.Enums;

namespace DonkeyWork.Agents.Persistence.Entities.Mcp;

/// <summary>
/// Configuration entity for HTTP-based MCP server connections.
/// </summary>
public class McpHttpConfigurationEntity
{
    /// <summary>
    /// Unique identifier (same as McpServerConfigurationId for 1:1 relationship).
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Foreign key to the parent MCP server configuration.
    /// </summary>
    public Guid McpServerConfigurationId { get; set; }

    /// <summary>
    /// Endpoint URL for the MCP server.
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// HTTP transport mode (AutoDetect, SSE, or StreamableHttp).
    /// </summary>
    public McpHttpTransportMode TransportMode { get; set; } = McpHttpTransportMode.AutoDetect;

    /// <summary>
    /// Authentication type (None, OAuth, or Header).
    /// </summary>
    public McpHttpAuthType AuthType { get; set; } = McpHttpAuthType.None;

    /// <summary>
    /// Optional reference to an OAuth token (connected account) for Bearer auth.
    /// </summary>
    public Guid? OAuthTokenId { get; set; }

    /// <summary>
    /// Navigation property to the parent configuration.
    /// </summary>
    public McpServerConfigurationEntity McpServerConfiguration { get; set; } = null!;

    /// <summary>
    /// Navigation property to OAuth configuration (when AuthType is OAuth).
    /// </summary>
    public McpHttpOAuthConfigurationEntity? OAuthConfiguration { get; set; }

    /// <summary>
    /// Navigation property to header configurations (when AuthType is Header).
    /// </summary>
    public ICollection<McpHttpHeaderConfigurationEntity> HeaderConfigurations { get; set; } = new List<McpHttpHeaderConfigurationEntity>();
}
