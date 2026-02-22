namespace DonkeyWork.Agents.Persistence.Entities.Research;

/// <summary>
/// Represents a research item with subject, findings, and associated notes.
/// </summary>
public class ResearchEntity : BaseEntity
{
    /// <summary>
    /// The research subject/question - the original ask.
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Detailed content/scope of the research (markdown supported).
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// Summary of research findings (populated when completing research).
    /// </summary>
    public string? Summary { get; set; }

    /// <summary>
    /// Research status.
    /// </summary>
    public ResearchStatus Status { get; set; } = ResearchStatus.NotStarted;

    /// <summary>
    /// Notes on completion (markdown supported).
    /// </summary>
    public string? CompletionNotes { get; set; }

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
