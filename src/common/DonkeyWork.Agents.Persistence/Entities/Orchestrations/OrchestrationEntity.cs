namespace DonkeyWork.Agents.Persistence.Entities.Orchestrations;

/// <summary>
/// Represents an orchestration with metadata and version tracking.
/// </summary>
public class OrchestrationEntity : BaseEntity
{
    /// <summary>
    /// Orchestration name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Orchestration description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Points to the latest published version. Null if no version has been published yet.
    /// </summary>
    public Guid? CurrentVersionId { get; set; }

    /// <summary>
    /// Navigation property to all versions of this orchestration.
    /// </summary>
    public ICollection<OrchestrationVersionEntity> Versions { get; set; } = new List<OrchestrationVersionEntity>();

    /// <summary>
    /// Navigation property to the current published version.
    /// </summary>
    public OrchestrationVersionEntity? CurrentVersion { get; set; }
}
