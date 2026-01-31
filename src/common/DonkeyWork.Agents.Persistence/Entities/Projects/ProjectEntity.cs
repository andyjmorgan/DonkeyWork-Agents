namespace DonkeyWork.Agents.Persistence.Entities.Projects;

/// <summary>
/// Represents a project with description, success criteria, milestones, todos, notes, tags and file references.
/// </summary>
public class ProjectEntity : BaseEntity
{
    /// <summary>
    /// Project name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Project description (markdown supported).
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Success criteria for the project (markdown supported).
    /// </summary>
    public string? SuccessCriteria { get; set; }

    /// <summary>
    /// Project status.
    /// </summary>
    public ProjectStatus Status { get; set; } = ProjectStatus.NotStarted;

    /// <summary>
    /// Navigation property to milestones.
    /// </summary>
    public ICollection<MilestoneEntity> Milestones { get; set; } = new List<MilestoneEntity>();

    /// <summary>
    /// Navigation property to todos directly under this project.
    /// </summary>
    public ICollection<TodoEntity> Todos { get; set; } = new List<TodoEntity>();

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
