namespace DonkeyWork.Agents.Persistence.Entities.Projects;

/// <summary>
/// Represents a tag associated with a milestone.
/// </summary>
public class MilestoneTagEntity : BaseEntity
{
    /// <summary>
    /// Tag name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Tag color (hex color code).
    /// </summary>
    public string? Color { get; set; }

    /// <summary>
    /// Foreign key to the parent milestone.
    /// </summary>
    public Guid MilestoneId { get; set; }

    /// <summary>
    /// Navigation property to the parent milestone.
    /// </summary>
    public MilestoneEntity Milestone { get; set; } = null!;
}
