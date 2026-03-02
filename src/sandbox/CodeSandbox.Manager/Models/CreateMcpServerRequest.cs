namespace CodeSandbox.Manager.Models;

public class CreateMcpServerRequest
{
    /// <summary>
    /// User ID that owns this MCP server pod.
    /// </summary>
    public required string UserId { get; set; }

    /// <summary>
    /// Configuration ID that identifies which MCP server this pod runs.
    /// Used with UserId for pod lookup (one pod per user per config).
    /// </summary>
    public required string McpServerConfigId { get; set; }

    /// <summary>
    /// The command/executable to run (e.g., "npx", "node", "python").
    /// If provided, the MCP process is started automatically after pod creation.
    /// </summary>
    public string? Command { get; set; }

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
    /// Environment variables to set in the container.
    /// </summary>
    public Dictionary<string, string>? EnvironmentVariables { get; set; }

    /// <summary>
    /// Working directory for the MCP process.
    /// </summary>
    public string? WorkingDirectory { get; set; }
}
