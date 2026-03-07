using DonkeyWork.Agents.Projects.Contracts.Models.Notifications;

namespace DonkeyWork.Agents.Projects.Contracts.Services;

/// <summary>
/// Service for sending typed project-related notifications to connected clients.
/// </summary>
public interface IProjectNotificationService
{
    /// <summary>
    /// Sends a notification when a task item's status changes.
    /// </summary>
    /// <param name="userId">The user ID to send the notification to.</param>
    /// <param name="notification">The task status changed notification payload.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SendTaskStatusChangedAsync(Guid userId, TaskStatusChangedNotification notification, CancellationToken ct = default);

    /// <summary>
    /// Sends a notification when a note is updated.
    /// </summary>
    /// <param name="userId">The user ID to send the notification to.</param>
    /// <param name="notification">The note updated notification payload.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SendNoteUpdatedAsync(Guid userId, NoteUpdatedNotification notification, CancellationToken ct = default);

    /// <summary>
    /// Sends a notification when a research item's status changes.
    /// </summary>
    /// <param name="userId">The user ID to send the notification to.</param>
    /// <param name="notification">The research status changed notification payload.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SendResearchStatusChangedAsync(Guid userId, ResearchStatusChangedNotification notification, CancellationToken ct = default);

    /// <summary>
    /// Sends a notification when a milestone is completed.
    /// </summary>
    /// <param name="userId">The user ID to send the notification to.</param>
    /// <param name="notification">The milestone completed notification payload.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SendMilestoneCompletedAsync(Guid userId, MilestoneCompletedNotification notification, CancellationToken ct = default);
}
