using DonkeyWork.Agents.Notifications.Contracts.Enums;

namespace DonkeyWork.Agents.Notifications.Contracts.Models;

/// <summary>
/// Represents a real-time notification sent to the frontend via SignalR.
/// </summary>
public class WorkspaceNotification
{
    /// <summary>
    /// The type of notification.
    /// </summary>
    public required NotificationType Type { get; init; }

    /// <summary>
    /// Human-readable title for the notification (displayed in toast header).
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Human-readable message for the notification (displayed in toast body).
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// The ID of the entity that was affected (project, milestone, task, or note ID).
    /// </summary>
    public required Guid EntityId { get; init; }

    /// <summary>
    /// Optional parent entity ID for hierarchical entities (e.g., projectId for milestones).
    /// </summary>
    public Guid? ParentId { get; init; }

    /// <summary>
    /// Timestamp when the notification was created.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
