namespace DonkeyWork.Agents.Persistence.Entities.Projects;

/// <summary>
/// Represents a todo item that can exist standalone or within a project/milestone.
/// </summary>
public class TodoEntity : BaseEntity
{
    /// <summary>
    /// Todo title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Todo description (markdown supported).
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Todo status.
    /// </summary>
    public TodoStatus Status { get; set; } = TodoStatus.Pending;

    /// <summary>
    /// Todo priority.
    /// </summary>
    public TodoPriority Priority { get; set; } = TodoPriority.Medium;

    /// <summary>
    /// Notes on completion (markdown supported).
    /// </summary>
    public string? CompletionNotes { get; set; }

    /// <summary>
    /// Due date for the todo.
    /// </summary>
    public DateTimeOffset? DueDate { get; set; }

    /// <summary>
    /// Date when the todo was completed.
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
    /// Navigation property to todo tags.
    /// </summary>
    public ICollection<TodoTagEntity> Tags { get; set; } = new List<TodoTagEntity>();
}

/// <summary>
/// Todo status enumeration.
/// </summary>
public enum TodoStatus
{
    Pending = 0,
    InProgress = 1,
    Completed = 2,
    Cancelled = 3
}

/// <summary>
/// Todo priority enumeration.
/// </summary>
public enum TodoPriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}
