namespace DonkeyWork.Agents.Persistence.Entities.Research;

/// <summary>
/// Represents a research item with subject, findings, and associated notes.
/// </summary>
public class ResearchEntity : BaseEntity
{
    /// <summary>
    /// The research title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// The research plan (markdown supported).
    /// </summary>
    public string? Plan { get; set; }

    /// <summary>
    /// Result of the research (populated when completing research).
    /// </summary>
    public string? Result { get; set; }

    /// <summary>
    /// Research status.
    /// </summary>
    public ResearchStatus Status { get; set; } = ResearchStatus.NotStarted;

    /// <summary>
    /// Date when the research was completed.
    /// </summary>
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>
    /// Navigation property to notes (research material/findings).
    /// </summary>
    public ICollection<Projects.NoteEntity> Notes { get; set; } = new List<Projects.NoteEntity>();

    /// <summary>
    /// Navigation property to tags.
    /// </summary>
    public ICollection<ResearchTagEntity> Tags { get; set; } = new List<ResearchTagEntity>();
}

/// <summary>
/// Research status enumeration.
/// </summary>
public enum ResearchStatus
{
    NotStarted = 0,
    InProgress = 1,
    Completed = 2,
    Cancelled = 3
}
