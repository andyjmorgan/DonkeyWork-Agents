using System.Text.Json;
using DonkeyWork.Agents.Orchestrations.Contracts.Models.ReactFlow;
using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Configurations;

namespace DonkeyWork.Agents.Persistence.Entities.Orchestrations;

/// <summary>
/// Represents a specific version of an orchestration with its configuration and schema.
/// </summary>
public class OrchestrationVersionEntity : BaseEntity
{
    public Guid OrchestrationId { get; set; }
    public int VersionNumber { get; set; }
    public bool IsDraft { get; set; }
    public JsonDocument InputSchema { get; set; } = JsonDocument.Parse("{}");
    public JsonDocument? OutputSchema { get; set; }
    public ReactFlowData ReactFlowData { get; set; } = new();
    public Dictionary<Guid, NodeConfiguration> NodeConfigurations { get; set; } = new();

    public bool DirectEnabled { get; set; } = true;
    public bool ToolEnabled { get; set; }
    public bool McpEnabled { get; set; }
    public bool NaviEnabled { get; set; }

    public DateTimeOffset? PublishedAt { get; set; }

    public OrchestrationEntity Orchestration { get; set; } = null!;
    public ICollection<OrchestrationVersionCredentialMappingEntity> CredentialMappings { get; set; } = new List<OrchestrationVersionCredentialMappingEntity>();
    public ICollection<OrchestrationExecutionEntity> Executions { get; set; } = new List<OrchestrationExecutionEntity>();
}
