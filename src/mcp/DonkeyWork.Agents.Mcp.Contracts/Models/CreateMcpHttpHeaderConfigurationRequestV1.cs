using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Mcp.Contracts.Models;

/// <summary>
/// Request to create a header configuration for an HTTP MCP server.
/// </summary>
public sealed class CreateMcpHttpHeaderConfigurationRequestV1
{
    [JsonPropertyName("headerName")]
    [Required]
    [StringLength(255, MinimumLength = 1)]
    public required string HeaderName { get; init; }

    [JsonPropertyName("headerValue")]
    [Required]
    [StringLength(4000, MinimumLength = 1)]
    public required string HeaderValue { get; init; }
}
