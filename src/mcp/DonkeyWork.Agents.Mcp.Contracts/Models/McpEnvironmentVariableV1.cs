using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Mcp.Contracts.Models;

/// <summary>
/// Environment variable configuration for an MCP stdio server.
/// Either a literal value or a credential store reference.
/// </summary>
public sealed class McpEnvironmentVariableV1
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("isCredentialReference")]
    public bool IsCredentialReference { get; init; }

    [JsonPropertyName("value")]
    public string? Value { get; init; }

    [JsonPropertyName("credentialId")]
    public Guid? CredentialId { get; init; }

    [JsonPropertyName("credentialFieldType")]
    public string? CredentialFieldType { get; init; }
}
