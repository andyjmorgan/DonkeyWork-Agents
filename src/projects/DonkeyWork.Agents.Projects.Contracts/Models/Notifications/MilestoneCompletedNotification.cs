namespace DonkeyWork.Agents.Projects.Contracts.Models.Notifications;

/// <summary>
/// Notification sent when a milestone is completed.
/// </summary>
public class MilestoneCompletedNotification
{
    /// <summary>
    /// The ID of the milestone that was completed.
    /// </summary>
    public required Guid MilestoneId { get; init; }

    /// <summary>
    /// The name of the milestone.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The name of the project the milestone belongs to.
    /// </summary>
    public required string ProjectName { get; init; }
}
