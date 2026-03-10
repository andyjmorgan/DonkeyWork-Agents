using System.Text.Json.Serialization;
using DonkeyWork.Agents.Common.Contracts.Enums;

namespace DonkeyWork.Agents.Mcp.Contracts.Models;

/// <summary>
/// MCP server configuration details response model.
/// </summary>
public sealed class McpServerDetailsV1
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("transportType")]
    public McpTransportType TransportType { get; init; }

    [JsonPropertyName("isEnabled")]
    public bool IsEnabled { get; init; }

    [JsonPropertyName("connectToNavi")]
    public bool ConnectToNavi { get; init; }

    [JsonPropertyName("stdioConfiguration")]
    public McpStdioConfigurationV1? StdioConfiguration { get; init; }

    [JsonPropertyName("httpConfiguration")]
    public McpHttpConfigurationV1? HttpConfiguration { get; init; }

    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public DateTimeOffset? UpdatedAt { get; init; }
}
