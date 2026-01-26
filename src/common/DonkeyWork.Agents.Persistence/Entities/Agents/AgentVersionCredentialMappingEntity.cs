namespace DonkeyWork.Agents.Persistence.Entities.Agents;

/// <summary>
/// Maps nodes in an agent version to credentials.
/// </summary>
public class AgentVersionCredentialMappingEntity : BaseEntity
{
    /// <summary>
    /// Foreign key to the agent version.
    /// </summary>
    public Guid AgentVersionId { get; set; }

    /// <summary>
    /// The node ID (GUID string) that references this credential.
    /// </summary>
    public string NodeId { get; set; } = string.Empty;

    /// <summary>
    /// Foreign key to the credential (ExternalApiKey).
    /// </summary>
    public Guid CredentialId { get; set; }

    /// <summary>
    /// Navigation property to the agent version.
    /// </summary>
    public AgentVersionEntity AgentVersion { get; set; } = null!;
}
