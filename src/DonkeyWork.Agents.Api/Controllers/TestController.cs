using Asp.Versioning;
using DonkeyWork.Agents.Notifications.Contracts.Enums;
using DonkeyWork.Agents.Notifications.Contracts.Interfaces;
using DonkeyWork.Agents.Notifications.Contracts.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DonkeyWork.Agents.Api.Controllers;

/// <summary>
/// Test endpoints for development and debugging.
/// </summary>
[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/test")]
[Authorize]
[Produces("application/json")]
public class TestController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public TestController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    /// <summary>
    /// Send a test notification via SignalR to the current user.
    /// </summary>
    /// <response code="200">Notification sent successfully.</response>
    [HttpPost("notification")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> SendTestNotification(CancellationToken cancellationToken)
    {
        var notification = new WorkspaceNotification
        {
            Type = NotificationType.ProjectCreated,
            Title = "Test Notification",
            Message = "This is a test notification sent via SignalR! 🎉",
            EntityId = Guid.NewGuid(),
            Timestamp = DateTimeOffset.UtcNow
        };

        await _notificationService.SendAsync(notification, cancellationToken);

        return Ok(new { message = "Test notification sent" });
    }
}
