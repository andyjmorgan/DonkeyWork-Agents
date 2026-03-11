namespace DonkeyWork.Agents.Projects.Contracts.Models.Notifications;

/// <summary>
/// Notification sent when a research item's status changes.
/// </summary>
public class ResearchStatusChangedNotification
{
    /// <summary>
    /// The ID of the research item whose status changed.
    /// </summary>
    public required Guid ResearchId { get; init; }

    /// <summary>
    /// The title of the research item.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// The new status of the research item.
    /// </summary>
    public required string Status { get; init; }
}
