using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Mcp.Contracts.Models;

/// <summary>
/// OAuth configuration for HTTP MCP server.
/// </summary>
public sealed class McpHttpOAuthConfigurationV1
{
    [JsonPropertyName("clientId")]
    public required string ClientId { get; init; }

    [JsonPropertyName("redirectUri")]
    public string? RedirectUri { get; init; }

    [JsonPropertyName("scopes")]
    public List<string> Scopes { get; init; } = [];

    [JsonPropertyName("authorizationEndpoint")]
    public string? AuthorizationEndpoint { get; init; }

    [JsonPropertyName("tokenEndpoint")]
    public string? TokenEndpoint { get; init; }
}
