using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using DonkeyWork.Agents.Orchestrations.Contracts.Models.Interfaces;
using DonkeyWork.Agents.Orchestrations.Contracts.Models.ReactFlow;

namespace DonkeyWork.Agents.Orchestrations.Contracts.Models;

/// <summary>
/// Request to save (create or update) an orchestration version draft.
/// </summary>
public sealed class SaveOrchestrationVersionRequestV1
{
    /// <summary>
    /// ReactFlow export data (nodes, edges, viewport).
    /// </summary>
    [JsonPropertyName("reactFlowData")]
    [Required]
    public required ReactFlowData ReactFlowData { get; init; }

    /// <summary>
    /// Node configurations keyed by node ID.
    /// Kept as JsonElement to allow polymorphic deserialization via NodeConfigurationRegistry.
    /// </summary>
    [JsonPropertyName("nodeConfigurations")]
    [Required]
    public required JsonElement NodeConfigurations { get; init; }

    /// <summary>
    /// JSON Schema for input validation.
    /// </summary>
    [JsonPropertyName("inputSchema")]
    [Required]
    public required JsonDocument InputSchema { get; init; }

    /// <summary>
    /// Optional JSON Schema for output.
    /// </summary>
    [JsonPropertyName("outputSchema")]
    public JsonDocument? OutputSchema { get; init; }

    /// <summary>
    /// Credential mappings (node ID to credential ID).
    /// </summary>
    [JsonPropertyName("credentialMappings")]
    public IReadOnlyList<CredentialMappingV1>? CredentialMappings { get; init; }

    /// <summary>
    /// Interface configurations for this version.
    /// An orchestration can support multiple interface types simultaneously.
    /// </summary>
    [JsonPropertyName("interfaces")]
    [Required]
    public required IList<InterfaceConfig> Interfaces { get; init; }
}

/// <summary>
/// Maps a node to a credential.
/// </summary>
public sealed class CredentialMappingV1
{
    /// <summary>
    /// Node ID.
    /// </summary>
    [JsonPropertyName("nodeId")]
    [Required]
    public required Guid NodeId { get; init; }

    /// <summary>
    /// Credential ID.
    /// </summary>
    [JsonPropertyName("credentialId")]
    [Required]
    public required Guid CredentialId { get; init; }
}
