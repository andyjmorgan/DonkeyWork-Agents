namespace DonkeyWork.Agents.Persistence.Entities.Projects;

/// <summary>
/// Represents a tag associated with a todo.
/// </summary>
public class TodoTagEntity : BaseEntity
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
    /// Foreign key to the parent todo.
    /// </summary>
    public Guid TodoId { get; set; }

    /// <summary>
    /// Navigation property to the parent todo.
    /// </summary>
    public TodoEntity Todo { get; set; } = null!;
}
