using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Common.Contracts.Enums;

/// <summary>
/// HTTP transport mode for MCP server connections.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum McpHttpTransportMode
{
    /// <summary>
    /// Automatically detect the transport mode (SSE or Streamable HTTP).
    /// </summary>
    AutoDetect = 0,

    /// <summary>
    /// Server-Sent Events transport (legacy).
    /// </summary>
    Sse = 1,

    /// <summary>
    /// Streamable HTTP transport (recommended).
    /// </summary>
    StreamableHttp = 2
}
