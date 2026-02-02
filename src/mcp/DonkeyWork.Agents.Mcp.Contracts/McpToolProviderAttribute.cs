namespace DonkeyWork.Agents.Mcp.Contracts;

/// <summary>
/// Specifies the provider of an MCP tool class.
/// Applied at the class level to identify which provider the tools belong to.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class McpToolProviderAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the provider of the tool.
    /// Defaults to <see cref="McpToolProvider.DonkeyWork"/>.
    /// </summary>
    public McpToolProvider Provider { get; set; } = McpToolProvider.DonkeyWork;
}
