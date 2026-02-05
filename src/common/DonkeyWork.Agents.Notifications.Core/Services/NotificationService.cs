using DonkeyWork.Agents.Identity.Contracts.Services;
using DonkeyWork.Agents.Notifications.Contracts.Interfaces;
using DonkeyWork.Agents.Notifications.Contracts.Models;
using DonkeyWork.Agents.Notifications.Core.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Notifications.Core.Services;

/// <summary>
/// Service for sending real-time notifications to connected clients via SignalR.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly IHubContext<NotificationHub, INotificationClient> _hubContext;
    private readonly IIdentityContext _identityContext;
    private readonly ILogger<NotificationService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationService"/> class.
    /// </summary>
    public NotificationService(
        IHubContext<NotificationHub, INotificationClient> hubContext,
        IIdentityContext identityContext,
        ILogger<NotificationService> logger)
    {
        _hubContext = hubContext;
        _identityContext = identityContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task SendAsync(WorkspaceNotification notification, CancellationToken cancellationToken = default)
    {
        var userId = _identityContext.UserId;
        return SendToUserAsync(userId, notification, cancellationToken);
    }

    /// <inheritdoc />
    public async Task SendToUserAsync(Guid userId, WorkspaceNotification notification, CancellationToken cancellationToken = default)
    {
        var groupName = $"user-{userId}";

        _logger.LogDebug(
            "Sending {NotificationType} notification to user {UserId} for entity {EntityId}",
            notification.Type,
            userId,
            notification.EntityId);

        try
        {
            await _hubContext.Clients
                .Group(groupName)
                .ReceiveNotification(notification);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to send {NotificationType} notification to user {UserId}",
                notification.Type,
                userId);
        }
    }
}
