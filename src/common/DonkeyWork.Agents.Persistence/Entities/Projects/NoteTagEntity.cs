namespace DonkeyWork.Agents.Persistence.Entities.Projects;

/// <summary>
/// Represents a tag associated with a note.
/// </summary>
public class NoteTagEntity : BaseEntity
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
    /// Foreign key to the parent note.
    /// </summary>
    public Guid NoteId { get; set; }

    /// <summary>
    /// Navigation property to the parent note.
    /// </summary>
    public NoteEntity Note { get; set; } = null!;
}
