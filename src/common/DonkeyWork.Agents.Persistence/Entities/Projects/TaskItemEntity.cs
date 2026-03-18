namespace DonkeyWork.Agents.Persistence.Entities.Projects;

/// <summary>
/// Represents a task item that can exist standalone or within a project/milestone.
/// </summary>
public class TaskItemEntity : BaseEntity
{
    /// <summary>
    /// Task item title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Task item description (markdown supported).
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Task item status.
    /// </summary>
    public TaskItemStatus Status { get; set; } = TaskItemStatus.Pending;

    /// <summary>
    /// Task item priority.
    /// </summary>
    public TaskItemPriority Priority { get; set; } = TaskItemPriority.Medium;

    /// <summary>
    /// Notes on completion (markdown supported).
    /// </summary>
    public string? CompletionNotes { get; set; }

    /// <summary>
    /// Date when the task item was completed.
    /// </summary>
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>
    /// Sort order within the parent.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Foreign key to the parent project (optional).
    /// </summary>
    public Guid? ProjectId { get; set; }

    /// <summary>
    /// Navigation property to the parent project.
    /// </summary>
    public ProjectEntity? Project { get; set; }

    /// <summary>
    /// Foreign key to the parent milestone (optional).
    /// </summary>
    public Guid? MilestoneId { get; set; }

    /// <summary>
    /// Navigation property to the parent milestone.
    /// </summary>
    public MilestoneEntity? Milestone { get; set; }

    /// <summary>
    /// Navigation property to task item tags.
    /// </summary>
    public ICollection<TaskItemTagEntity> Tags { get; set; } = new List<TaskItemTagEntity>();
}

/// <summary>
/// Task item status enumeration.
/// </summary>
public enum TaskItemStatus
{
    Pending = 0,
    InProgress = 1,
    Completed = 2,
    Cancelled = 3
}

/// <summary>
/// Task item priority enumeration.
/// </summary>
public enum TaskItemPriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}
