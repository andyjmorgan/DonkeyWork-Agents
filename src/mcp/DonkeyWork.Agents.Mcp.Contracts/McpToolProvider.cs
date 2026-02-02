namespace DonkeyWork.Agents.Mcp.Contracts;

/// <summary>
/// Identifies the provider of an MCP tool.
/// </summary>
public enum McpToolProvider
{
    /// <summary>
    /// Native DonkeyWork tools.
    /// </summary>
    DonkeyWork = 0,

    /// <summary>
    /// Microsoft-provided tools.
    /// </summary>
    Microsoft = 1,

    /// <summary>
    /// Google-provided tools.
    /// </summary>
    Google = 2
}
