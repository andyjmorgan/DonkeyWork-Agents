namespace DonkeyWork.Agents.Persistence.Entities.Research;

/// <summary>
/// Represents a tag associated with a research item.
/// </summary>
public class ResearchTagEntity : BaseEntity
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
    /// Foreign key to the parent research.
    /// </summary>
    public Guid ResearchId { get; set; }

    /// <summary>
    /// Navigation property to the parent research.
    /// </summary>
    public ResearchEntity Research { get; set; } = null!;
}
