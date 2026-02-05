using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Mcp.Contracts.Models;

/// <summary>
/// Request to create an OAuth configuration for an HTTP MCP server.
/// </summary>
public sealed class CreateMcpHttpOAuthConfigurationRequestV1
{
    [JsonPropertyName("clientId")]
    [Required]
    [StringLength(255, MinimumLength = 1)]
    public required string ClientId { get; init; }

    [JsonPropertyName("clientSecret")]
    [Required]
    [StringLength(500, MinimumLength = 1)]
    public required string ClientSecret { get; init; }

    [JsonPropertyName("redirectUri")]
    [StringLength(2000)]
    [Url]
    public string? RedirectUri { get; init; }

    [JsonPropertyName("scopes")]
    public List<string>? Scopes { get; init; }

    [JsonPropertyName("authorizationEndpoint")]
    [StringLength(2000)]
    [Url]
    public string? AuthorizationEndpoint { get; init; }

    [JsonPropertyName("tokenEndpoint")]
    [StringLength(2000)]
    [Url]
    public string? TokenEndpoint { get; init; }
}
