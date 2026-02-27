using DonkeyWork.Agents.Common.Contracts.Enums;

namespace DonkeyWork.Agents.Mcp.Contracts.Models;

/// <summary>
/// Connection-ready MCP server configuration with decrypted secrets.
/// </summary>
public sealed class McpConnectionConfigV1
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
    /// HTTP endpoint URL for the MCP server.
    /// </summary>
    public required string Endpoint { get; init; }

    /// <summary>
    /// HTTP transport mode (AutoDetect, SSE, or StreamableHttp).
    /// </summary>
    public McpHttpTransportMode TransportMode { get; init; }

    /// <summary>
    /// Decrypted headers as name-value pairs.
    /// </summary>
    public Dictionary<string, string> Headers { get; init; } = new();
}
