using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using DonkeyWork.Agents.Common.Contracts.Enums;

namespace DonkeyWork.Agents.Mcp.Contracts.Models;

/// <summary>
/// Request to create an HTTP configuration for an MCP server.
/// </summary>
public sealed class CreateMcpHttpConfigurationRequestV1
{
    [JsonPropertyName("endpoint")]
    [Required]
    [StringLength(2000, MinimumLength = 1)]
    [Url]
    public required string Endpoint { get; init; }

    [JsonPropertyName("transportMode")]
    public McpHttpTransportMode TransportMode { get; init; } = McpHttpTransportMode.AutoDetect;

    [JsonPropertyName("authType")]
    public McpHttpAuthType AuthType { get; init; } = McpHttpAuthType.None;

    [JsonPropertyName("oauthConfiguration")]
    public CreateMcpHttpOAuthConfigurationRequestV1? OAuthConfiguration { get; init; }

    [JsonPropertyName("oauthTokenId")]
    public Guid? OAuthTokenId { get; init; }

    [JsonPropertyName("headerConfigurations")]
    public List<CreateMcpHttpHeaderConfigurationRequestV1>? HeaderConfigurations { get; init; }
}
