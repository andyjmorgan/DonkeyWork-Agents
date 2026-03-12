namespace DonkeyWork.Agents.Mcp.Contracts.Models;

/// <summary>
/// Response from testing an MCP server connection.
/// </summary>
public sealed class TestMcpServerResponseV1
{
    /// <summary>
    /// Whether the connection test was successful.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// The server name reported by the MCP server.
    /// </summary>
    public string? ServerName { get; init; }

    /// <summary>
    /// The server version reported by the MCP server.
    /// </summary>
    public string? ServerVersion { get; init; }

    /// <summary>
    /// Time elapsed in milliseconds for the test.
    /// </summary>
    public long ElapsedMs { get; init; }

    /// <summary>
    /// Tools discovered on the server.
    /// </summary>
    public IReadOnlyList<McpToolInfoV1> Tools { get; init; } = [];

    /// <summary>
    /// Error message if the test failed.
    /// </summary>
    public string? Error { get; init; }
}
