using System.Text.Json;
using DonkeyWork.Agents.Orchestrations.Contracts.Models.Interfaces;
using DonkeyWork.Agents.Orchestrations.Contracts.Models.ReactFlow;
using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Configurations;

namespace DonkeyWork.Agents.Persistence.Entities.Orchestrations;

/// <summary>
/// Represents a specific version of an orchestration with its configuration and schema.
/// </summary>
public class OrchestrationVersionEntity : BaseEntity
{
    /// <summary>
    /// Foreign key to the parent orchestration.
    /// </summary>
    public Guid OrchestrationId { get; set; }

    /// <summary>
    /// Incrementing version number.
    /// </summary>
    public int VersionNumber { get; set; }

    /// <summary>
    /// Whether this is an unpublished draft version.
    /// </summary>
    public bool IsDraft { get; set; }

    /// <summary>
    /// JSON Schema for input validation. Stored as JSONB.
    /// </summary>
    public JsonDocument InputSchema { get; set; } = JsonDocument.Parse("{}");

    /// <summary>
    /// Optional JSON Schema for output. Stored as JSONB.
    /// </summary>
    public JsonDocument? OutputSchema { get; set; }

    /// <summary>
    /// Complete ReactFlow graph data. Stored as JSONB.
    /// </summary>
    public ReactFlowData ReactFlowData { get; set; } = new();

    /// <summary>
    /// Dictionary of node configurations keyed by node ID. Stored as JSONB.
    /// </summary>
    public Dictionary<Guid, NodeConfiguration> NodeConfigurations { get; set; } = new();

    /// <summary>
    /// Interface configurations for this version. Stored as JSONB.
    /// </summary>
    public OrchestrationInterfaces Interfaces { get; set; } = new();

    /// <summary>
    /// Timestamp when this version was published. Null for drafts.
    /// </summary>
    public DateTimeOffset? PublishedAt { get; set; }

    /// <summary>
    /// Navigation property to the parent orchestration.
    /// </summary>
    public OrchestrationEntity Orchestration { get; set; } = null!;

    /// <summary>
    /// Navigation property to credential mappings.
    /// </summary>
    public ICollection<OrchestrationVersionCredentialMappingEntity> CredentialMappings { get; set; } = new List<OrchestrationVersionCredentialMappingEntity>();

    /// <summary>
    /// Navigation property to executions using this version.
    /// </summary>
    public ICollection<OrchestrationExecutionEntity> Executions { get; set; } = new List<OrchestrationExecutionEntity>();
}
