using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Mcp.Contracts.Models;

/// <summary>
/// Request to create an environment variable for an MCP stdio server.
/// Provide either a literal value or a credential reference (credentialId + credentialFieldType).
/// </summary>
public sealed class CreateMcpEnvironmentVariableRequestV1
{
    [JsonPropertyName("name")]
    [Required]
    [StringLength(255, MinimumLength = 1)]
    public required string Name { get; init; }

    [JsonPropertyName("value")]
    public string? Value { get; init; }

    [JsonPropertyName("credentialId")]
    public Guid? CredentialId { get; init; }

    [JsonPropertyName("credentialFieldType")]
    [StringLength(50)]
    public string? CredentialFieldType { get; init; }
}
