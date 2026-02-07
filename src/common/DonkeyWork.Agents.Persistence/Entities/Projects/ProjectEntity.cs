namespace DonkeyWork.Agents.Persistence.Entities.Projects;

/// <summary>
/// Represents a project with description, success criteria, milestones, task items, notes, tags and file references.
/// </summary>
public class ProjectEntity : BaseEntity
{
    /// <summary>
    /// Project name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Project content - a write-up of the project's scope (markdown supported).
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// Project status.
    /// </summary>
    public ProjectStatus Status { get; set; } = ProjectStatus.NotStarted;

    /// <summary>
    /// Navigation property to milestones.
    /// </summary>
    public ICollection<MilestoneEntity> Milestones { get; set; } = new List<MilestoneEntity>();

    /// <summary>
    /// Navigation property to task items directly under this project.
    /// </summary>
    public ICollection<TaskItemEntity> TaskItems { get; set; } = new List<TaskItemEntity>();

    /// <summary>
    /// Navigation property to notes directly under this project.
    /// </summary>
    public ICollection<NoteEntity> Notes { get; set; } = new List<NoteEntity>();

    /// <summary>
    /// Navigation property to project tags.
    /// </summary>
    public ICollection<ProjectTagEntity> Tags { get; set; } = new List<ProjectTagEntity>();

    /// <summary>
    /// Navigation property to file references.
    /// </summary>
    public ICollection<ProjectFileReferenceEntity> FileReferences { get; set; } = new List<ProjectFileReferenceEntity>();
}

/// <summary>
/// Project status enumeration.
/// </summary>
public enum ProjectStatus
{
    NotStarted = 0,
    InProgress = 1,
    OnHold = 2,
    Completed = 3,
    Cancelled = 4
}
