namespace DonkeyWork.Agents.Persistence.Entities.Projects;

/// <summary>
/// Represents a milestone within a project with similar structure to projects.
/// </summary>
public class MilestoneEntity : BaseEntity
{
    /// <summary>
    /// Milestone name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Milestone content - a write-up of the milestone's scope (markdown supported).
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// Success criteria for the milestone (markdown supported).
    /// </summary>
    public string? SuccessCriteria { get; set; }

    /// <summary>
    /// Milestone status.
    /// </summary>
    public MilestoneStatus Status { get; set; } = MilestoneStatus.NotStarted;

    /// <summary>
    /// Target completion date for the milestone.
    /// </summary>
    public DateTimeOffset? DueDate { get; set; }

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

    /// <summary>
    /// Navigation property to task items under this milestone.
    /// </summary>
    public ICollection<TaskItemEntity> TaskItems { get; set; } = new List<TaskItemEntity>();

    /// <summary>
    /// Navigation property to notes under this milestone.
    /// </summary>
    public ICollection<NoteEntity> Notes { get; set; } = new List<NoteEntity>();

    /// <summary>
    /// Navigation property to milestone tags.
    /// </summary>
    public ICollection<MilestoneTagEntity> Tags { get; set; } = new List<MilestoneTagEntity>();

    /// <summary>
    /// Navigation property to file references.
    /// </summary>
    public ICollection<MilestoneFileReferenceEntity> FileReferences { get; set; } = new List<MilestoneFileReferenceEntity>();
}

/// <summary>
/// Milestone status enumeration.
/// </summary>
public enum MilestoneStatus
{
    NotStarted = 0,
    InProgress = 1,
    OnHold = 2,
    Completed = 3,
    Cancelled = 4
}
