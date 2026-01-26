namespace DonkeyWork.Agents.Persistence.Entities.Agents;

/// <summary>
/// Represents an agent with metadata and version tracking.
/// </summary>
public class AgentEntity : BaseEntity
{
    /// <summary>
    /// Agent name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Agent description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Points to the latest published version. Null if no version has been published yet.
    /// </summary>
    public Guid? CurrentVersionId { get; set; }

    /// <summary>
    /// Navigation property to all versions of this agent.
    /// </summary>
    public ICollection<AgentVersionEntity> Versions { get; set; } = new List<AgentVersionEntity>();

    /// <summary>
    /// Navigation property to the current published version.
    /// </summary>
    public AgentVersionEntity? CurrentVersion { get; set; }
}
