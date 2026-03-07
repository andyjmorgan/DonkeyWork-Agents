namespace DonkeyWork.Agents.Projects.Contracts.Models.Notifications;

/// <summary>
/// Notification sent when a task item's status changes.
/// </summary>
public class TaskStatusChangedNotification
{
    /// <summary>
    /// The ID of the task item whose status changed.
    /// </summary>
    public required Guid TaskId { get; init; }

    /// <summary>
    /// The title of the task item.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// The previous status of the task item.
    /// </summary>
    public required string OldStatus { get; init; }

    /// <summary>
    /// The new status of the task item.
    /// </summary>
    public required string NewStatus { get; init; }
}
