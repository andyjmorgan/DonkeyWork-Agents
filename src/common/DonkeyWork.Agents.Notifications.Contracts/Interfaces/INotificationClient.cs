using DonkeyWork.Agents.Notifications.Contracts.Models;

namespace DonkeyWork.Agents.Notifications.Contracts.Interfaces;

/// <summary>
/// SignalR hub client interface defining methods that can be called on connected clients.
/// Used with strongly-typed hubs: Hub&lt;INotificationClient&gt;.
/// </summary>
public interface INotificationClient
{
    /// <summary>
    /// Receives a workspace notification (project, milestone, task, or note change).
    /// </summary>
    /// <param name="notification">The notification payload.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ReceiveNotification(WorkspaceNotification notification);
}
