namespace DonkeyWork.Agents.Persistence.Entities.Projects;

/// <summary>
/// Represents a tag associated with a project.
/// </summary>
public class ProjectTagEntity : BaseEntity
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
    /// Foreign key to the parent project.
    /// </summary>
    public Guid ProjectId { get; set; }

    /// <summary>
    /// Navigation property to the parent project.
    /// </summary>
    public ProjectEntity Project { get; set; } = null!;
}
