using DonkeyWork.Agents.Notifications.Contracts.Enums;
using DonkeyWork.Agents.Notifications.Contracts.Interfaces;
using DonkeyWork.Agents.Notifications.Contracts.Models;
using DonkeyWork.Agents.Projects.Contracts.Models.Notifications;
using DonkeyWork.Agents.Projects.Contracts.Services;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Projects.Core.Services;

/// <summary>
/// Service for sending typed project-related notifications via the shared notification infrastructure.
/// </summary>
public class ProjectNotificationService : IProjectNotificationService
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<ProjectNotificationService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectNotificationService"/> class.
    /// </summary>
    public ProjectNotificationService(
        INotificationService notificationService,
        ILogger<ProjectNotificationService> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task SendTaskStatusChangedAsync(Guid userId, TaskStatusChangedNotification notification, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Sending TaskStatusChanged notification for task {TaskId}: {OldStatus} -> {NewStatus}",
            notification.TaskId,
            notification.OldStatus,
            notification.NewStatus);

        return _notificationService.SendToUserAsync(userId, new WorkspaceNotification
        {
            Type = NotificationType.TaskUpdated,
            Title = $"Task {notification.NewStatus}",
            Message = $"'{notification.Title}' changed from {notification.OldStatus} to {notification.NewStatus}",
            EntityId = notification.TaskId
        }, ct);
    }

    /// <inheritdoc />
    public Task SendNoteUpdatedAsync(Guid userId, NoteUpdatedNotification notification, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Sending NoteUpdated notification for note {NoteId}",
            notification.NoteId);

        return _notificationService.SendToUserAsync(userId, new WorkspaceNotification
        {
            Type = NotificationType.NoteUpdated,
            Title = "Note Updated",
            Message = $"Note '{notification.Title}' has been updated",
            EntityId = notification.NoteId
        }, ct);
    }

    /// <inheritdoc />
    public Task SendResearchStatusChangedAsync(Guid userId, ResearchStatusChangedNotification notification, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Sending ResearchStatusChanged notification for research {ResearchId}: {Status}",
            notification.ResearchId,
            notification.Status);

        return _notificationService.SendToUserAsync(userId, new WorkspaceNotification
        {
            Type = NotificationType.ProjectUpdated,
            Title = $"Research {notification.Status}",
            Message = $"'{notification.Title}' is now {notification.Status}",
            EntityId = notification.ResearchId
        }, ct);
    }

    /// <inheritdoc />
    public Task SendMilestoneCompletedAsync(Guid userId, MilestoneCompletedNotification notification, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Sending MilestoneCompleted notification for milestone {MilestoneId} in project {ProjectName}",
            notification.MilestoneId,
            notification.ProjectName);

        return _notificationService.SendToUserAsync(userId, new WorkspaceNotification
        {
            Type = NotificationType.MilestoneUpdated,
            Title = "Milestone Completed",
            Message = $"Milestone '{notification.Name}' in project '{notification.ProjectName}' has been completed",
            EntityId = notification.MilestoneId
        }, ct);
    }
}
