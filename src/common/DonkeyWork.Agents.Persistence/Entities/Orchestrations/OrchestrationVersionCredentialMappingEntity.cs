namespace DonkeyWork.Agents.Persistence.Entities.Orchestrations;

/// <summary>
/// Maps nodes in an orchestration version to credentials.
/// </summary>
public class OrchestrationVersionCredentialMappingEntity : BaseEntity
{
    /// <summary>
    /// Foreign key to the orchestration version.
    /// </summary>
    public Guid OrchestrationVersionId { get; set; }

    /// <summary>
    /// The node ID that references this credential.
    /// </summary>
    public Guid NodeId { get; set; }

    /// <summary>
    /// Foreign key to the credential (ExternalApiKey).
    /// </summary>
    public Guid CredentialId { get; set; }

    /// <summary>
    /// Navigation property to the orchestration version.
    /// </summary>
    public OrchestrationVersionEntity OrchestrationVersion { get; set; } = null!;
}
