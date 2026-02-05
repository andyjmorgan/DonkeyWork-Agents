using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Common.Contracts.Enums;

/// <summary>
/// Transport type for MCP server connections.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum McpTransportType
{
    /// <summary>
    /// Standard I/O transport for locally spawned MCP servers.
    /// </summary>
    Stdio = 0,

    /// <summary>
    /// HTTP transport for remote MCP servers (supports SSE and Streamable HTTP).
    /// </summary>
    Http = 1
}
