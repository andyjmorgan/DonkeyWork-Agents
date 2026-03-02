namespace CodeSandbox.Manager.Models;

/// <summary>
/// Request to start (arm) the MCP process inside an already-running container.
/// </summary>
public class McpStartRequest
{
    /// <summary>
    /// The command/executable to run (e.g., "npx", "node", "python").
    /// </summary>
    public required string Command { get; set; }

    /// <summary>
    /// Arguments to pass to the command.
    /// </summary>
    public string[] Arguments { get; set; } = [];

    /// <summary>
    /// Optional scripts to run before launching the MCP server.
    /// </summary>
    public string[] PreExecScripts { get; set; } = [];

    /// <summary>
    /// Timeout in seconds for the MCP server to start. Default: 30.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Working directory for the MCP process.
    /// </summary>
    public string? WorkingDirectory { get; set; }
}
