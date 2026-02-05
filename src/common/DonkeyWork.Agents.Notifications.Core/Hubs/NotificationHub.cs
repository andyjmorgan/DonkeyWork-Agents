using DonkeyWork.Agents.Notifications.Contracts.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace DonkeyWork.Agents.Notifications.Core.Hubs;

/// <summary>
/// SignalR hub for real-time workspace notifications.
/// Clients are automatically added to a user-specific group on connection.
/// </summary>
[Authorize]
public class NotificationHub : Hub<INotificationClient>
{
    /// <summary>
    /// Called when a client connects to the hub.
    /// Adds the client to a user-specific group for targeted notifications.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst("sub")?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
        }

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects from the hub.
    /// Groups are automatically cleaned up by SignalR.
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }
}
