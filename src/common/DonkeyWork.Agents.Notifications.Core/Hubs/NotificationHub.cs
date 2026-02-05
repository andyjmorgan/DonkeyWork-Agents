using DonkeyWork.Agents.Notifications.Contracts.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Notifications.Core.Hubs;

/// <summary>
/// SignalR hub for real-time workspace notifications.
/// Clients are automatically added to a user-specific group on connection.
/// </summary>
[Authorize(AuthenticationSchemes = "Bearer")]
public class NotificationHub : Hub<INotificationClient>
{
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(ILogger<NotificationHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Called when a client connects to the hub.
    /// Adds the client to a user-specific group for targeted notifications.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst("sub")?.Value;
        var groupName = $"user-{userId}";

        _logger.LogInformation(
            "Client {ConnectionId} connected. UserId from sub claim: {UserId}, adding to group: {GroupName}",
            Context.ConnectionId,
            userId ?? "(null)",
            groupName);

        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            _logger.LogInformation("Client {ConnectionId} added to group {GroupName}", Context.ConnectionId, groupName);
        }
        else
        {
            _logger.LogWarning("Client {ConnectionId} has no 'sub' claim, not adding to any group", Context.ConnectionId);
        }

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects from the hub.
    /// Groups are automatically cleaned up by SignalR.
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client {ConnectionId} disconnected. Error: {Error}", Context.ConnectionId, exception?.Message ?? "(none)");
        await base.OnDisconnectedAsync(exception);
    }
}
