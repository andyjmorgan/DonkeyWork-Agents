using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Mcp.Contracts.Models;

/// <summary>
/// Header configuration for HTTP MCP server.
/// </summary>
public sealed class McpHttpHeaderConfigurationV1
{
    [JsonPropertyName("headerName")]
    public required string HeaderName { get; init; }

    [JsonPropertyName("headerValue")]
    public required string HeaderValue { get; init; }
}
