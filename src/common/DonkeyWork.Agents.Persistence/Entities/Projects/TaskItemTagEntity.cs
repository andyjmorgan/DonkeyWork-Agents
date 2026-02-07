namespace DonkeyWork.Agents.Persistence.Entities.Projects;

/// <summary>
/// Represents a tag associated with a task item.
/// </summary>
public class TaskItemTagEntity : BaseEntity
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
    /// Foreign key to the parent task item.
    /// </summary>
    public Guid TaskItemId { get; set; }

    /// <summary>
    /// Navigation property to the parent task item.
    /// </summary>
    public TaskItemEntity TaskItem { get; set; } = null!;
}
