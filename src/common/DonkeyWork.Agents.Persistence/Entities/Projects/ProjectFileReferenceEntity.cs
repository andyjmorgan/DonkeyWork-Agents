namespace DonkeyWork.Agents.Persistence.Entities.Projects;

/// <summary>
/// Represents a file reference within a project.
/// </summary>
public class ProjectFileReferenceEntity : BaseEntity
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
    /// Sort order within the project.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Foreign key to the parent project.
    /// </summary>
    public Guid ProjectId { get; set; }

    /// <summary>
    /// Navigation property to the parent project.
    /// </summary>
    public ProjectEntity Project { get; set; } = null!;
}
