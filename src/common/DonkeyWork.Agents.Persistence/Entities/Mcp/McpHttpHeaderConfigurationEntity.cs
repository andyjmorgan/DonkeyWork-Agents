namespace DonkeyWork.Agents.Persistence.Entities.Mcp;

/// <summary>
/// Header configuration entity for HTTP-based MCP server connections.
/// </summary>
public class McpHttpHeaderConfigurationEntity
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Foreign key to the parent HTTP configuration.
    /// </summary>
    public Guid McpHttpConfigurationId { get; set; }

    /// <summary>
    /// HTTP header name.
    /// </summary>
    public string HeaderName { get; set; } = string.Empty;

    /// <summary>
    /// HTTP header value (encrypted).
    /// </summary>
    public string HeaderValueEncrypted { get; set; } = string.Empty;

    /// <summary>
    /// Navigation property to the parent HTTP configuration.
    /// </summary>
    public McpHttpConfigurationEntity McpHttpConfiguration { get; set; } = null!;
}
