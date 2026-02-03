using System.Text.Json;
using System.Text.Json.Serialization;
using DonkeyWork.Agents.Orchestrations.Contracts.Models.Interfaces;
using DonkeyWork.Agents.Orchestrations.Contracts.Models.ReactFlow;

namespace DonkeyWork.Agents.Orchestrations.Contracts.Models;

/// <summary>
/// Response containing full agent version data.
/// </summary>
public sealed class GetOrchestrationVersionResponseV1
{
    /// <summary>
    /// Version ID.
    /// </summary>
    [JsonPropertyName("id")]
    public required Guid Id { get; init; }

    /// <summary>
    /// Orchestration ID.
    /// </summary>
    [JsonPropertyName("orchestrationId")]
    public required Guid OrchestrationId { get; init; }

    /// <summary>
    /// Version number.
    /// </summary>
    [JsonPropertyName("versionNumber")]
    public required int VersionNumber { get; init; }

    /// <summary>
    /// Whether this is an unpublished draft.
    /// </summary>
    [JsonPropertyName("isDraft")]
    public required bool IsDraft { get; init; }

    /// <summary>
    /// JSON Schema for input validation.
    /// </summary>
    [JsonPropertyName("inputSchema")]
    public required JsonElement InputSchema { get; init; }

    /// <summary>
    /// Optional JSON Schema for output.
    /// </summary>
    [JsonPropertyName("outputSchema")]
    public JsonElement? OutputSchema { get; init; }

    /// <summary>
    /// ReactFlow export data.
    /// </summary>
    [JsonPropertyName("reactFlowData")]
    public required ReactFlowData ReactFlowData { get; init; }

    /// <summary>
    /// Node configurations keyed by node ID.
    /// Kept as JsonElement for frontend compatibility.
    /// </summary>
    [JsonPropertyName("nodeConfigurations")]
    public required JsonElement NodeConfigurations { get; init; }

    /// <summary>
    /// Creation timestamp.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Publication timestamp (null for drafts).
    /// </summary>
    [JsonPropertyName("publishedAt")]
    public DateTimeOffset? PublishedAt { get; init; }

    /// <summary>
    /// Interface configurations for this version (MCP, A2A, Chat, Webhook).
    /// </summary>
    [JsonPropertyName("interfaces")]
    public OrchestrationInterfaces? Interfaces { get; init; }
}
