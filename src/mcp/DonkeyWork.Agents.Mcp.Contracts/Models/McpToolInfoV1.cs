namespace DonkeyWork.Agents.Mcp.Contracts.Models;

/// <summary>
/// Information about a tool discovered from an MCP server.
/// </summary>
public sealed class McpToolInfoV1
{
    /// <summary>
    /// The tool name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The tool description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// The JSON schema for the tool's input parameters.
    /// </summary>
    public object? InputSchema { get; init; }
}
