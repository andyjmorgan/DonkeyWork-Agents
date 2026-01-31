namespace DonkeyWork.Agents.Persistence.Entities.Projects;

/// <summary>
/// Represents a file reference within a milestone.
/// </summary>
public class MilestoneFileReferenceEntity : BaseEntity
{
    /// <summary>
    /// File path or URL.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Display name for the file reference.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Optional description of the file.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Sort order within the milestone.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Foreign key to the parent milestone.
    /// </summary>
    public Guid MilestoneId { get; set; }

    /// <summary>
    /// Navigation property to the parent milestone.
    /// </summary>
    public MilestoneEntity Milestone { get; set; } = null!;
}
