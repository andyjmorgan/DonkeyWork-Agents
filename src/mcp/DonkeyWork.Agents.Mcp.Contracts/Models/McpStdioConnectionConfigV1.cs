namespace DonkeyWork.Agents.Mcp.Contracts.Models;

/// <summary>
/// Connection-ready configuration for a stdio MCP server.
/// Contains everything needed to create and connect to the server via the sandbox manager.
/// </summary>
public sealed class McpStdioConnectionConfigV1
{
    /// <summary>
    /// The MCP server configuration ID.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Display name of the MCP server.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The command to execute (e.g., "python", "npx", "node").
    /// </summary>
    public required string Command { get; init; }

    /// <summary>
    /// Command arguments.
    /// </summary>
    public List<string> Arguments { get; init; } = [];

    /// <summary>
    /// Environment variables to set for the MCP process.
    /// </summary>
    public Dictionary<string, string> EnvironmentVariables { get; init; } = new();

    /// <summary>
    /// Scripts to execute before starting the MCP process (e.g., pip install).
    /// </summary>
    public List<string> PreExecScripts { get; init; } = [];

    /// <summary>
    /// Optional working directory for the MCP process.
    /// </summary>
    public string? WorkingDirectory { get; init; }
}
