namespace DonkeyWork.Agents.Mcp.Contracts;

/// <summary>
/// Specifies metadata for an MCP tool method.
/// Applied at the method level to provide tool information and MCP standard annotations.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class McpToolAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the unique name of the tool.
    /// This is the identifier used by MCP clients to invoke the tool.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the human-readable title of the tool.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the description of what the tool does.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the icon identifier for the tool.
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Gets or sets the OAuth scopes required to use this tool.
    /// </summary>
    public string[]? RequiredScopes { get; set; }

    /// <summary>
    /// Gets or sets whether the tool only reads data and does not modify state.
    /// MCP Standard Annotation.
    /// </summary>
    public bool ReadOnlyHint { get; set; }

    /// <summary>
    /// Gets or sets whether the tool performs destructive operations (e.g., delete).
    /// MCP Standard Annotation.
    /// </summary>
    public bool DestructiveHint { get; set; }

    /// <summary>
    /// Gets or sets whether calling the tool multiple times with the same parameters has the same effect.
    /// MCP Standard Annotation.
    /// </summary>
    public bool IdempotentHint { get; set; }

    /// <summary>
    /// Gets or sets whether the tool interacts with external systems beyond the local environment.
    /// MCP Standard Annotation.
    /// </summary>
    public bool OpenWorldHint { get; set; }
}
