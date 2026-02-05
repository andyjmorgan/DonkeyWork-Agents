using DonkeyWork.Agents.Notifications.Contracts.Models;

namespace DonkeyWork.Agents.Notifications.Contracts.Interfaces;

/// <summary>
/// Service interface for sending real-time notifications to connected clients.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Sends a notification to the current authenticated user.
    /// Uses IIdentityContext internally to determine the user.
    /// </summary>
    /// <param name="notification">The notification payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SendAsync(WorkspaceNotification notification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a notification to a specific user.
    /// Use this when you need to notify a user other than the current authenticated user.
    /// </summary>
    /// <param name="userId">The user ID to send the notification to.</param>
    /// <param name="notification">The notification payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SendToUserAsync(Guid userId, WorkspaceNotification notification, CancellationToken cancellationToken = default);
}
