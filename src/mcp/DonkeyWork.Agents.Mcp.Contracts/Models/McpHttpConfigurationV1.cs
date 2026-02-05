using System.Text.Json.Serialization;
using DonkeyWork.Agents.Common.Contracts.Enums;

namespace DonkeyWork.Agents.Mcp.Contracts.Models;

/// <summary>
/// HTTP configuration for MCP server.
/// </summary>
public sealed class McpHttpConfigurationV1
{
    [JsonPropertyName("endpoint")]
    public required string Endpoint { get; init; }

    [JsonPropertyName("transportMode")]
    public McpHttpTransportMode TransportMode { get; init; }

    [JsonPropertyName("authType")]
    public McpHttpAuthType AuthType { get; init; }

    [JsonPropertyName("oauthConfiguration")]
    public McpHttpOAuthConfigurationV1? OAuthConfiguration { get; init; }

    [JsonPropertyName("headerConfigurations")]
    public List<McpHttpHeaderConfigurationV1> HeaderConfigurations { get; init; } = [];
}
