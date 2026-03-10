using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using DonkeyWork.Agents.Common.Contracts.Enums;

namespace DonkeyWork.Agents.Mcp.Contracts.Models;

/// <summary>
/// Request to create an MCP server configuration.
/// </summary>
public sealed class CreateMcpServerRequestV1
{
    [JsonPropertyName("name")]
    [Required]
    [StringLength(255, MinimumLength = 1)]
    public required string Name { get; init; }

    [JsonPropertyName("description")]
    [StringLength(2000)]
    public string? Description { get; init; }

    [JsonPropertyName("transportType")]
    [Required]
    public McpTransportType TransportType { get; init; }

    [JsonPropertyName("isEnabled")]
    public bool IsEnabled { get; init; } = true;

    [JsonPropertyName("connectToNavi")]
    public bool ConnectToNavi { get; init; }

    [JsonPropertyName("stdioConfiguration")]
    public CreateMcpStdioConfigurationRequestV1? StdioConfiguration { get; init; }

    [JsonPropertyName("httpConfiguration")]
    public CreateMcpHttpConfigurationRequestV1? HttpConfiguration { get; init; }
}
