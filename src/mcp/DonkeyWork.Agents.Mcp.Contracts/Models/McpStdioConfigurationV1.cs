using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Mcp.Contracts.Models;

/// <summary>
/// Stdio configuration for MCP server.
/// </summary>
public sealed class McpStdioConfigurationV1
{
    [JsonPropertyName("command")]
    public required string Command { get; init; }

    [JsonPropertyName("arguments")]
    public List<string> Arguments { get; init; } = [];

    [JsonPropertyName("environmentVariables")]
    public List<McpEnvironmentVariableV1> EnvironmentVariables { get; init; } = [];

    [JsonPropertyName("preExecScripts")]
    public List<string> PreExecScripts { get; init; } = [];

    [JsonPropertyName("workingDirectory")]
    public string? WorkingDirectory { get; init; }
}
