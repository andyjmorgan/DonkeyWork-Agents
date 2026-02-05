namespace DonkeyWork.Agents.Persistence.Entities.Mcp;

/// <summary>
/// OAuth configuration entity for HTTP-based MCP server connections.
/// </summary>
public class McpHttpOAuthConfigurationEntity
{
    /// <summary>
    /// Unique identifier (same as McpHttpConfigurationId for 1:1 relationship).
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Foreign key to the parent HTTP configuration.
    /// </summary>
    public Guid McpHttpConfigurationId { get; set; }

    /// <summary>
    /// OAuth client ID.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// OAuth client secret (encrypted).
    /// </summary>
    public string ClientSecretEncrypted { get; set; } = string.Empty;

    /// <summary>
    /// OAuth redirect URI.
    /// </summary>
    public string? RedirectUri { get; set; }

    /// <summary>
    /// OAuth scopes (JSON array).
    /// </summary>
    public string Scopes { get; set; } = "[]";

    /// <summary>
    /// OAuth authorization endpoint.
    /// </summary>
    public string? AuthorizationEndpoint { get; set; }

    /// <summary>
    /// OAuth token endpoint.
    /// </summary>
    public string? TokenEndpoint { get; set; }

    /// <summary>
    /// Navigation property to the parent HTTP configuration.
    /// </summary>
    public McpHttpConfigurationEntity McpHttpConfiguration { get; set; } = null!;
}
