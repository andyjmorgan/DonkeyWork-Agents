namespace DonkeyWork.Agents.Persistence.Entities.Mcp;

/// <summary>
/// Configuration entity for stdio-based MCP server connections.
/// </summary>
public class McpStdioConfigurationEntity
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
    /// Command to execute (e.g., "python", "npx", "node").
    /// </summary>
    public string Command { get; set; } = string.Empty;

    /// <summary>
    /// Arguments to pass to the command (JSON array).
    /// </summary>
    public string Arguments { get; set; } = "[]";

    /// <summary>
    /// Environment variable configurations (literal or credential references).
    /// </summary>
    public ICollection<McpStdioEnvironmentVariableEntity> EnvironmentVariableConfigurations { get; set; } = [];

    /// <summary>
    /// Pre-execution scripts to run before starting the MCP server (JSON array).
    /// </summary>
    public string PreExecScripts { get; set; } = "[]";

    /// <summary>
    /// Working directory for the process.
    /// </summary>
    public string? WorkingDirectory { get; set; }

    /// <summary>
    /// Navigation property to the parent configuration.
    /// </summary>
    public McpServerConfigurationEntity McpServerConfiguration { get; set; } = null!;
}
