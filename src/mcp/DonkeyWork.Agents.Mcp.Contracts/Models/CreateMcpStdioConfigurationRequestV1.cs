using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Mcp.Contracts.Models;

/// <summary>
/// Request to create a stdio configuration for an MCP server.
/// </summary>
public sealed class CreateMcpStdioConfigurationRequestV1
{
    [JsonPropertyName("command")]
    [Required]
    [StringLength(255, MinimumLength = 1)]
    public required string Command { get; init; }

    [JsonPropertyName("arguments")]
    public List<string>? Arguments { get; init; }

    [JsonPropertyName("environmentVariables")]
    public Dictionary<string, string>? EnvironmentVariables { get; init; }

    [JsonPropertyName("preExecScripts")]
    public List<string>? PreExecScripts { get; init; }

    [JsonPropertyName("workingDirectory")]
    [StringLength(1000)]
    public string? WorkingDirectory { get; init; }
}
