namespace DonkeyWork.Agents.Persistence.Entities.Mcp;

/// <summary>
/// Environment variable configuration for stdio-based MCP server connections.
/// Supports both literal values and credential store references.
/// </summary>
public class McpStdioEnvironmentVariableEntity
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Foreign key to the parent stdio configuration.
    /// </summary>
    public Guid McpStdioConfigurationId { get; set; }

    /// <summary>
    /// Environment variable name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Literal value (null when using a credential reference).
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// Reference to an external API key credential (null when using a literal value).
    /// </summary>
    public Guid? CredentialId { get; set; }

    /// <summary>
    /// The field type to extract from the referenced credential (e.g. "ApiKey", "Password").
    /// Stored as string to avoid cross-module enum dependency.
    /// </summary>
    public string? CredentialFieldType { get; set; }

    /// <summary>
    /// Navigation property to the parent stdio configuration.
    /// </summary>
    public McpStdioConfigurationEntity McpStdioConfiguration { get; set; } = null!;
}
