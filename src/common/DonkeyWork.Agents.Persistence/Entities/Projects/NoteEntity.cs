namespace DonkeyWork.Agents.Persistence.Entities.Projects;

/// <summary>
/// Represents a note that can exist standalone or within a project/milestone.
/// </summary>
public class NoteEntity : BaseEntity
{
    /// <summary>
    /// Note title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Note content (markdown supported).
    /// </summary>
    public string? Content { get; set; }

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
    /// Foreign key to the parent research (optional).
    /// </summary>
    public Guid? ResearchId { get; set; }

    /// <summary>
    /// Navigation property to the parent research.
    /// </summary>
    public DonkeyWork.Agents.Persistence.Entities.Research.ResearchEntity? Research { get; set; }

    /// <summary>
    /// Navigation property to note tags.
    /// </summary>
    public ICollection<NoteTagEntity> Tags { get; set; } = new List<NoteTagEntity>();
}
