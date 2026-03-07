using DonkeyWork.Agents.Identity.Contracts.Services;
using DonkeyWork.Agents.Notifications.Contracts.Enums;
using DonkeyWork.Agents.Notifications.Contracts.Interfaces;
using DonkeyWork.Agents.Notifications.Contracts.Models;
using DonkeyWork.Agents.Persistence;
using DonkeyWork.Agents.Persistence.Entities.Projects;
using DonkeyWork.Agents.Projects.Contracts.Helpers;
using DonkeyWork.Agents.Projects.Contracts.Models;
using DonkeyWork.Agents.Projects.Contracts.Models.Notifications;
using DonkeyWork.Agents.Projects.Contracts.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Projects.Core.Services;

public class MilestoneService : IMilestoneService
{
    private readonly AgentsDbContext _dbContext;
    private readonly IIdentityContext _identityContext;
    private readonly INotificationService _notificationService;
    private readonly IProjectNotificationService _projectNotificationService;
    private readonly ILogger<MilestoneService> _logger;

    public MilestoneService(
        AgentsDbContext dbContext,
        IIdentityContext identityContext,
        INotificationService notificationService,
        IProjectNotificationService projectNotificationService,
        ILogger<MilestoneService> logger)
    {
        _dbContext = dbContext;
        _identityContext = identityContext;
        _notificationService = notificationService;
        _projectNotificationService = projectNotificationService;
        _logger = logger;
    }

    public async Task<MilestoneDetailsV1?> CreateAsync(Guid projectId, CreateMilestoneRequestV1 request, CancellationToken cancellationToken = default)
    {
        var userId = _identityContext.UserId;

        // Verify project exists
        var projectExists = await _dbContext.Projects.AnyAsync(p => p.Id == projectId, cancellationToken);
        if (!projectExists)
        {
            return null;
        }

        _logger.LogInformation("Creating milestone for project {ProjectId} with name {Name}", projectId, request.Name);

        var milestoneId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var milestone = new MilestoneEntity
        {
            Id = milestoneId,
            UserId = userId,
            ProjectId = projectId,
            Name = request.Name,
            Content = request.Content,
            SuccessCriteria = request.SuccessCriteria,
            Status = (Persistence.Entities.Projects.MilestoneStatus)(int)request.Status,
            DueDate = request.DueDate,
            SortOrder = request.SortOrder,
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.Milestones.Add(milestone);

        // Add tags
        if (request.Tags?.Any() == true)
        {
            foreach (var tag in request.Tags)
            {
                _dbContext.MilestoneTags.Add(new MilestoneTagEntity
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    MilestoneId = milestoneId,
                    Name = tag.Name,
                    Color = tag.Color,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }
        }

        // Add file references
        if (request.FileReferences?.Any() == true)
        {
            foreach (var fileRef in request.FileReferences)
            {
                _dbContext.MilestoneFileReferences.Add(new MilestoneFileReferenceEntity
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    MilestoneId = milestoneId,
                    FilePath = fileRef.FilePath,
                    DisplayName = fileRef.DisplayName,
                    Description = fileRef.Description,
                    SortOrder = fileRef.SortOrder,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created milestone {MilestoneId}", milestoneId);

        // Send notification (fire-and-forget)
        _ = _notificationService.SendAsync(new WorkspaceNotification
        {
            Type = NotificationType.MilestoneCreated,
            Title = "Milestone Created",
            Message = $"Milestone '{request.Name}' has been created",
            EntityId = milestoneId,
            ParentId = projectId
        });

        return await GetByIdAsync(milestoneId, cancellationToken: cancellationToken);
    }

    public async Task<MilestoneDetailsV1?> GetByIdAsync(Guid milestoneId, int? contentOffset = null, int? contentLength = null, CancellationToken cancellationToken = default)
    {
        var milestone = await _dbContext.Milestones
            .AsNoTracking()
            .Include(m => m.Tags)
            .Include(m => m.FileReferences)
            .Include(m => m.TaskItems)
                .ThenInclude(t => t.Tags)
            .Include(m => m.Notes)
                .ThenInclude(n => n.Tags)
            .FirstOrDefaultAsync(m => m.Id == milestoneId, cancellationToken);

        return milestone == null ? null : MapToDetails(milestone, contentOffset, contentLength);
    }

    public async Task<IReadOnlyList<MilestoneSummaryV1>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var milestones = await _dbContext.Milestones
            .AsNoTracking()
            .Include(m => m.Tags)
            .Include(m => m.TaskItems)
            .Where(m => m.ProjectId == projectId)
            .OrderBy(m => m.SortOrder)
            .ToListAsync(cancellationToken);

        return milestones.Select(MapToSummary).ToList();
    }

    public async Task<MilestoneDetailsV1?> UpdateAsync(Guid milestoneId, UpdateMilestoneRequestV1 request, CancellationToken cancellationToken = default)
    {
        var userId = _identityContext.UserId;
        var milestone = await _dbContext.Milestones
            .Include(m => m.Tags)
            .Include(m => m.FileReferences)
            .FirstOrDefaultAsync(m => m.Id == milestoneId, cancellationToken);

        if (milestone == null)
        {
            return null;
        }

        // Validate completion notes are provided when moving to a terminal status
        var isTerminalStatus = request.Status is Contracts.Models.MilestoneStatus.Completed or Contracts.Models.MilestoneStatus.Cancelled;
        if (isTerminalStatus && string.IsNullOrWhiteSpace(request.CompletionNotes))
        {
            throw new InvalidOperationException("Completion notes are required when marking a milestone as completed or cancelled.");
        }

        var now = DateTimeOffset.UtcNow;
        var oldStatus = milestone.Status;
        var newStatus = (Persistence.Entities.Projects.MilestoneStatus)(int)request.Status;
        var wasTerminal = oldStatus is Persistence.Entities.Projects.MilestoneStatus.Completed or Persistence.Entities.Projects.MilestoneStatus.Cancelled;
        var isNowTerminal = newStatus is Persistence.Entities.Projects.MilestoneStatus.Completed or Persistence.Entities.Projects.MilestoneStatus.Cancelled;

        milestone.Name = request.Name;
        milestone.Content = request.Content;
        milestone.SuccessCriteria = request.SuccessCriteria;
        milestone.Status = newStatus;
        milestone.CompletionNotes = request.CompletionNotes;
        milestone.DueDate = request.DueDate;
        milestone.SortOrder = request.SortOrder;
        milestone.UpdatedAt = now;

        // Set completed timestamp when moving to terminal status
        if (!wasTerminal && isNowTerminal)
        {
            milestone.CompletedAt = now;
        }
        else if (wasTerminal && !isNowTerminal)
        {
            milestone.CompletedAt = null;
        }

        // Update tags - remove existing and add new
        _dbContext.MilestoneTags.RemoveRange(milestone.Tags);
        if (request.Tags?.Any() == true)
        {
            foreach (var tag in request.Tags)
            {
                _dbContext.MilestoneTags.Add(new MilestoneTagEntity
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    MilestoneId = milestoneId,
                    Name = tag.Name,
                    Color = tag.Color,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }
        }

        // Update file references - remove existing and add new
        _dbContext.MilestoneFileReferences.RemoveRange(milestone.FileReferences);
        if (request.FileReferences?.Any() == true)
        {
            foreach (var fileRef in request.FileReferences)
            {
                _dbContext.MilestoneFileReferences.Add(new MilestoneFileReferenceEntity
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    MilestoneId = milestoneId,
                    FilePath = fileRef.FilePath,
                    DisplayName = fileRef.DisplayName,
                    Description = fileRef.Description,
                    SortOrder = fileRef.SortOrder,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated milestone {MilestoneId}", milestoneId);

        // Send notification (fire-and-forget)
        var statusChanged = oldStatus != newStatus;
        var notificationMessage = statusChanged
            ? $"'{request.Name}' is now {FormatStatus(request.Status)}"
            : $"'{request.Name}' has been updated";

        _ = _notificationService.SendAsync(new WorkspaceNotification
        {
            Type = NotificationType.MilestoneUpdated,
            Title = statusChanged ? $"Milestone {FormatStatus(request.Status)}" : "Milestone Updated",
            Message = notificationMessage,
            EntityId = milestoneId,
            ParentId = milestone.ProjectId
        });

        // Send typed notification when milestone status changed to Completed
        if (statusChanged && newStatus == Persistence.Entities.Projects.MilestoneStatus.Completed)
        {
            var projectName = await _dbContext.Projects
                .Where(p => p.Id == milestone.ProjectId)
                .Select(p => p.Name)
                .FirstOrDefaultAsync(cancellationToken) ?? "Unknown";

            _ = _projectNotificationService.SendMilestoneCompletedAsync(
                _identityContext.UserId,
                new MilestoneCompletedNotification
                {
                    MilestoneId = milestoneId,
                    Name = request.Name,
                    ProjectName = projectName
                });
        }

        return await GetByIdAsync(milestoneId, cancellationToken: cancellationToken);
    }

    private static string FormatStatus(Contracts.Models.MilestoneStatus status) => status switch
    {
        Contracts.Models.MilestoneStatus.NotStarted => "Not Started",
        Contracts.Models.MilestoneStatus.InProgress => "In Progress",
        Contracts.Models.MilestoneStatus.OnHold => "On Hold",
        Contracts.Models.MilestoneStatus.Completed => "Completed",
        Contracts.Models.MilestoneStatus.Cancelled => "Cancelled",
        _ => status.ToString()
    };

    public async Task<bool> DeleteAsync(Guid milestoneId, CancellationToken cancellationToken = default)
    {
        var userId = _identityContext.UserId;
        var milestone = await _dbContext.Milestones
            .FirstOrDefaultAsync(m => m.Id == milestoneId, cancellationToken);

        if (milestone == null)
        {
            return false;
        }

        var milestoneName = milestone.Name;
        var projectId = milestone.ProjectId;
        _dbContext.Milestones.Remove(milestone);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted milestone {MilestoneId}", milestoneId);

        // Send notification (fire-and-forget)
        _ = _notificationService.SendAsync(new WorkspaceNotification
        {
            Type = NotificationType.MilestoneDeleted,
            Title = "Milestone Deleted",
            Message = $"Milestone '{milestoneName}' has been deleted",
            EntityId = milestoneId,
            ParentId = projectId
        });

        return true;
    }

    private static MilestoneSummaryV1 MapToSummary(MilestoneEntity milestone)
    {
        return new MilestoneSummaryV1
        {
            Id = milestone.Id,
            ProjectId = milestone.ProjectId,
            Name = milestone.Name,
            Status = (Contracts.Models.MilestoneStatus)(int)milestone.Status,
            DueDate = milestone.DueDate,
            SortOrder = milestone.SortOrder,
            Tags = milestone.Tags.Select(t => new TagV1 { Id = t.Id, Name = t.Name, Color = t.Color }).ToList(),
            TaskItemCount = milestone.TaskItems.Count,
            CompletedTaskItemCount = milestone.TaskItems.Count(t => t.Status == Persistence.Entities.Projects.TaskItemStatus.Completed),
            ContentPreview = ContentTruncationHelper.TruncateContent(milestone.Content),
            ContentLength = ContentTruncationHelper.GetContentLength(milestone.Content),
            CompletedAt = milestone.CompletedAt,
            CreatedAt = milestone.CreatedAt,
            UpdatedAt = milestone.UpdatedAt
        };
    }

    private static MilestoneDetailsV1 MapToDetails(MilestoneEntity milestone, int? contentOffset = null, int? contentLength = null)
    {
        return new MilestoneDetailsV1
        {
            Id = milestone.Id,
            ProjectId = milestone.ProjectId,
            Name = milestone.Name,
            Content = ContentTruncationHelper.ApplyChunking(milestone.Content, contentOffset, contentLength),
            ContentLength = ContentTruncationHelper.GetContentLength(milestone.Content),
            SuccessCriteria = ContentTruncationHelper.ApplyChunking(milestone.SuccessCriteria, contentOffset, contentLength),
            Status = (Contracts.Models.MilestoneStatus)(int)milestone.Status,
            CompletionNotes = milestone.CompletionNotes,
            CompletedAt = milestone.CompletedAt,
            DueDate = milestone.DueDate,
            SortOrder = milestone.SortOrder,
            Tags = milestone.Tags.Select(t => new TagV1 { Id = t.Id, Name = t.Name, Color = t.Color }).ToList(),
            FileReferences = milestone.FileReferences.OrderBy(f => f.SortOrder).Select(f => new FileReferenceV1
            {
                Id = f.Id,
                FilePath = f.FilePath,
                DisplayName = f.DisplayName,
                Description = f.Description,
                SortOrder = f.SortOrder
            }).ToList(),
            Tasks = milestone.TaskItems.OrderBy(t => t.SortOrder).Select(t => new TaskItemV1
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                DescriptionLength = ContentTruncationHelper.GetContentLength(t.Description),
                Status = (Contracts.Models.TaskItemStatus)(int)t.Status,
                Priority = (Contracts.Models.TaskItemPriority)(int)t.Priority,
                CompletionNotes = t.CompletionNotes,
                DueDate = t.DueDate,
                CompletedAt = t.CompletedAt,
                SortOrder = t.SortOrder,
                ProjectId = t.ProjectId,
                MilestoneId = t.MilestoneId,
                Tags = t.Tags.Select(tag => new TagV1 { Id = tag.Id, Name = tag.Name, Color = tag.Color }).ToList(),
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt
            }).ToList(),
            Notes = milestone.Notes.OrderBy(n => n.SortOrder).Select(n => new NoteV1
            {
                Id = n.Id,
                Title = n.Title,
                Content = n.Content,
                ContentLength = ContentTruncationHelper.GetContentLength(n.Content),
                SortOrder = n.SortOrder,
                ProjectId = n.ProjectId,
                MilestoneId = n.MilestoneId,
                Tags = n.Tags.Select(tag => new TagV1 { Id = tag.Id, Name = tag.Name, Color = tag.Color }).ToList(),
                CreatedAt = n.CreatedAt,
                UpdatedAt = n.UpdatedAt
            }).ToList(),
            CreatedAt = milestone.CreatedAt,
            UpdatedAt = milestone.UpdatedAt
        };
    }
}
