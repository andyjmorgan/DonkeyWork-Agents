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
    public string InputSchema { get; set; } = string.Empty;

    /// <summary>
    /// Optional JSON Schema for output. Stored as JSONB.
    /// </summary>
    public string? OutputSchema { get; set; }

    /// <summary>
    /// Complete ReactFlow export data. Stored as JSONB.
    /// </summary>
    public string ReactFlowData { get; set; } = string.Empty;

    /// <summary>
    /// Dictionary of node configurations keyed by node ID. Stored as JSONB.
    /// </summary>
    public string NodeConfigurations { get; set; } = string.Empty;

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
