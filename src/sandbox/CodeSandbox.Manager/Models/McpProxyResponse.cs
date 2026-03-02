namespace CodeSandbox.Manager.Models;

/// <summary>
/// Response from proxying a JSON-RPC message to the MCP server.
/// </summary>
public class McpProxyResponse
{
    /// <summary>
    /// Raw JSON-RPC response body.
    /// </summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// True if the original request was a notification (no response expected).
    /// </summary>
    public bool IsNotification { get; set; }
}
