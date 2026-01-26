using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Agents.Contracts.Models;

/// <summary>
/// Request to save (create or update) an agent version draft.
/// </summary>
public sealed class SaveAgentVersionRequestV1
{
    /// <summary>
    /// ReactFlow export data (nodes, edges, viewport).
    /// </summary>
    [JsonPropertyName("reactFlowData")]
    [Required]
    public required JsonElement ReactFlowData { get; init; }

    /// <summary>
    /// Node configurations keyed by node ID.
    /// </summary>
    [JsonPropertyName("nodeConfigurations")]
    [Required]
    public required JsonElement NodeConfigurations { get; init; }

    /// <summary>
    /// JSON Schema for input validation.
    /// </summary>
    [JsonPropertyName("inputSchema")]
    [Required]
    public required JsonElement InputSchema { get; init; }

    /// <summary>
    /// Optional JSON Schema for output.
    /// </summary>
    [JsonPropertyName("outputSchema")]
    public JsonElement? OutputSchema { get; init; }

    /// <summary>
    /// Credential mappings (node ID to credential ID).
    /// </summary>
    [JsonPropertyName("credentialMappings")]
    public IReadOnlyList<CredentialMappingV1>? CredentialMappings { get; init; }
}

/// <summary>
/// Maps a node to a credential.
/// </summary>
public sealed class CredentialMappingV1
{
    /// <summary>
    /// Node ID (GUID string).
    /// </summary>
    [JsonPropertyName("nodeId")]
    [Required]
    public required string NodeId { get; init; }

    /// <summary>
    /// Credential ID.
    /// </summary>
    [JsonPropertyName("credentialId")]
    [Required]
    public required Guid CredentialId { get; init; }
}
