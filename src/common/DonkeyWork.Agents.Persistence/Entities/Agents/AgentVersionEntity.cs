using System.Text.Json;
using DonkeyWork.Agents.Agents.Contracts.Models.ReactFlow;
using DonkeyWork.Agents.Agents.Contracts.Nodes.Configurations;

namespace DonkeyWork.Agents.Persistence.Entities.Agents;

/// <summary>
/// Represents a specific version of an agent with its configuration and schema.
/// </summary>
public class AgentVersionEntity : BaseEntity
{
    /// <summary>
    /// Foreign key to the parent agent.
    /// </summary>
    public Guid AgentId { get; set; }

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
    /// Timestamp when this version was published. Null for drafts.
    /// </summary>
    public DateTimeOffset? PublishedAt { get; set; }

    /// <summary>
    /// Navigation property to the parent agent.
    /// </summary>
    public AgentEntity Agent { get; set; } = null!;

    /// <summary>
    /// Navigation property to credential mappings.
    /// </summary>
    public ICollection<AgentVersionCredentialMappingEntity> CredentialMappings { get; set; } = new List<AgentVersionCredentialMappingEntity>();

    /// <summary>
    /// Navigation property to executions using this version.
    /// </summary>
    public ICollection<AgentExecutionEntity> Executions { get; set; } = new List<AgentExecutionEntity>();
}
