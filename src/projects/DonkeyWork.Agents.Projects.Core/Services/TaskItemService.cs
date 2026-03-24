using DonkeyWork.Agents.Common.Contracts.Models.Pagination;
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

public class TaskItemService : ITaskItemService
{
    private readonly AgentsDbContext _dbContext;
    private readonly IIdentityContext _identityContext;
    private readonly INotificationService _notificationService;
    private readonly IProjectNotificationService _projectNotificationService;
    private readonly ILogger<TaskItemService> _logger;

    public TaskItemService(
        AgentsDbContext dbContext,
        IIdentityContext identityContext,
        INotificationService notificationService,
        IProjectNotificationService projectNotificationService,
        ILogger<TaskItemService> logger)
    {
        _dbContext = dbContext;
        _identityContext = identityContext;
        _notificationService = notificationService;
        _projectNotificationService = projectNotificationService;
        _logger = logger;
    }

    public async Task<TaskItemV1> CreateAsync(CreateTaskItemRequestV1 request, CancellationToken cancellationToken = default)
    {
        var userId = _identityContext.UserId;
        _logger.LogInformation("Creating task item for user {UserId} with title {Title}", userId, request.Title);

        var taskItemId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var taskItem = new TaskItemEntity
        {
            Id = taskItemId,
            UserId = userId,
            Title = request.Title,
            Description = request.Description,
            Status = (Persistence.Entities.Projects.TaskItemStatus)(int)request.Status,
            Priority = (Persistence.Entities.Projects.TaskItemPriority)(int)request.Priority,
            SortOrder = request.SortOrder,
            ProjectId = request.ProjectId,
            MilestoneId = request.MilestoneId,
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.TaskItems.Add(taskItem);

        // Add tags
        if (request.Tags?.Any() == true)
        {
            foreach (var tag in request.Tags)
            {
                _dbContext.TaskItemTags.Add(new TaskItemTagEntity
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    TaskItemId = taskItemId,
                    Name = tag.Name,
                    Color = tag.Color,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created task item {TaskItemId}", taskItemId);

        // Send notification (fire-and-forget)
        _ = _notificationService.SendAsync(new WorkspaceNotification
        {
            Type = NotificationType.TaskCreated,
            Title = "Task Created",
            Message = $"Task '{request.Title}' has been created",
            EntityId = taskItemId,
            ParentId = request.MilestoneId ?? request.ProjectId
        });

        return (await GetByIdAsync(taskItemId, cancellationToken: cancellationToken))!;
    }

    public async Task<TaskItemV1?> GetByIdAsync(Guid taskItemId, int? contentOffset = null, int? contentLength = null, CancellationToken cancellationToken = default)
    {
        var taskItem = await _dbContext.TaskItems
            .AsNoTracking()
            .Include(t => t.Tags)
            .FirstOrDefaultAsync(t => t.Id == taskItemId, cancellationToken);

        return taskItem == null ? null : MapToDto(taskItem, contentOffset, contentLength);
    }

    public async Task<IReadOnlyList<TaskItemSummaryV1>> GetStandaloneAsync(CancellationToken cancellationToken = default)
    {
        var taskItems = await _dbContext.TaskItems
            .AsNoTracking()
            .Include(t => t.Tags)
            .Where(t => t.ProjectId == null && t.MilestoneId == null)
            .OrderBy(t => t.SortOrder)
            .ThenByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);

        return taskItems.Select(MapToSummaryDto).ToList();
    }

    public async Task<PaginatedResponse<TaskItemSummaryV1>> ListAsync(PaginationRequest pagination, TaskItemFilterRequestV1? filter = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.TaskItems
            .AsNoTracking()
            .Include(t => t.Tags)
            .AsQueryable();

        query = ApplyFilters(query, filter);

        var totalCount = await query.CountAsync(cancellationToken);

        var taskItems = await query
            .OrderBy(t => t.SortOrder)
            .ThenByDescending(t => t.CreatedAt)
            .Skip(pagination.Offset)
            .Take(pagination.GetLimit())
            .ToListAsync(cancellationToken);

        return new PaginatedResponse<TaskItemSummaryV1>
        {
            Items = taskItems.Select(MapToSummaryDto).ToList(),
            Offset = pagination.Offset,
            Limit = pagination.GetLimit(),
            TotalCount = totalCount
        };
    }

    public async Task<PaginatedResponse<TaskItemSummaryV1>> GetByProjectIdAsync(Guid projectId, PaginationRequest pagination, TaskItemFilterRequestV1? filter = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.TaskItems
            .AsNoTracking()
            .Include(t => t.Tags)
            .Where(t => t.ProjectId == projectId);

        query = ApplyFilters(query, filter);

        var totalCount = await query.CountAsync(cancellationToken);

        var taskItems = await query
            .OrderBy(t => t.SortOrder)
            .ThenByDescending(t => t.CreatedAt)
            .Skip(pagination.Offset)
            .Take(pagination.GetLimit())
            .ToListAsync(cancellationToken);

        return new PaginatedResponse<TaskItemSummaryV1>
        {
            Items = taskItems.Select(MapToSummaryDto).ToList(),
            Offset = pagination.Offset,
            Limit = pagination.GetLimit(),
            TotalCount = totalCount
        };
    }

    public async Task<PaginatedResponse<TaskItemSummaryV1>> GetByMilestoneIdAsync(Guid milestoneId, PaginationRequest pagination, TaskItemFilterRequestV1? filter = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.TaskItems
            .AsNoTracking()
            .Include(t => t.Tags)
            .Where(t => t.MilestoneId == milestoneId);

        query = ApplyFilters(query, filter);

        var totalCount = await query.CountAsync(cancellationToken);

        var taskItems = await query
            .OrderBy(t => t.SortOrder)
            .ThenByDescending(t => t.CreatedAt)
            .Skip(pagination.Offset)
            .Take(pagination.GetLimit())
            .ToListAsync(cancellationToken);

        return new PaginatedResponse<TaskItemSummaryV1>
        {
            Items = taskItems.Select(MapToSummaryDto).ToList(),
            Offset = pagination.Offset,
            Limit = pagination.GetLimit(),
            TotalCount = totalCount
        };
    }

    private static IQueryable<TaskItemEntity> ApplyFilters(IQueryable<TaskItemEntity> query, TaskItemFilterRequestV1? filter)
    {
        if (filter == null)
            return query;

        if (filter.Status.HasValue)
        {
            var entityStatus = (Persistence.Entities.Projects.TaskItemStatus)(int)filter.Status.Value;
            query = query.Where(t => t.Status == entityStatus);
        }

        if (filter.Priority.HasValue)
        {
            var entityPriority = (Persistence.Entities.Projects.TaskItemPriority)(int)filter.Priority.Value;
            query = query.Where(t => t.Priority == entityPriority);
        }

        return query;
    }

    public async Task<TaskItemV1?> UpdateAsync(Guid taskItemId, UpdateTaskItemRequestV1 request, CancellationToken cancellationToken = default)
    {
        var userId = _identityContext.UserId;
        var taskItem = await _dbContext.TaskItems
            .Include(t => t.Tags)
            .FirstOrDefaultAsync(t => t.Id == taskItemId, cancellationToken);

        if (taskItem == null)
        {
            return null;
        }

        // Validate completion notes are provided when moving to a terminal status
        var isTerminalStatus = request.Status is Contracts.Models.TaskItemStatus.Completed or Contracts.Models.TaskItemStatus.Cancelled;
        if (isTerminalStatus && string.IsNullOrWhiteSpace(request.CompletionNotes))
        {
            throw new InvalidOperationException("Completion notes are required when marking a task as completed or cancelled.");
        }

        var now = DateTimeOffset.UtcNow;
        var oldStatus = taskItem.Status;
        var newStatus = (Persistence.Entities.Projects.TaskItemStatus)(int)request.Status;
        var wasTerminal = oldStatus is Persistence.Entities.Projects.TaskItemStatus.Completed or Persistence.Entities.Projects.TaskItemStatus.Cancelled;
        var isNowTerminal = newStatus is Persistence.Entities.Projects.TaskItemStatus.Completed or Persistence.Entities.Projects.TaskItemStatus.Cancelled;

        taskItem.Title = request.Title;
        taskItem.Description = request.Description;
        taskItem.Status = newStatus;
        taskItem.Priority = (Persistence.Entities.Projects.TaskItemPriority)(int)request.Priority;
        taskItem.CompletionNotes = request.CompletionNotes;
        taskItem.SortOrder = request.SortOrder;
        taskItem.ProjectId = request.ProjectId;
        taskItem.MilestoneId = request.MilestoneId;
        taskItem.UpdatedAt = now;

        // Set completed timestamp when moving to terminal status
        if (!wasTerminal && isNowTerminal)
        {
            taskItem.CompletedAt = now;
        }
        else if (wasTerminal && !isNowTerminal)
        {
            taskItem.CompletedAt = null;
        }

        // Update tags - remove existing and add new
        _dbContext.TaskItemTags.RemoveRange(taskItem.Tags);
        if (request.Tags?.Any() == true)
        {
            foreach (var tag in request.Tags)
            {
                _dbContext.TaskItemTags.Add(new TaskItemTagEntity
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    TaskItemId = taskItemId,
                    Name = tag.Name,
                    Color = tag.Color,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated task item {TaskItemId}", taskItemId);

        // Send notification (fire-and-forget)
        var statusChanged = oldStatus != newStatus;
        var notificationMessage = statusChanged
            ? $"'{request.Title}' is now {FormatStatus(request.Status)}"
            : $"'{request.Title}' has been updated";

        _ = _notificationService.SendAsync(new WorkspaceNotification
        {
            Type = NotificationType.TaskUpdated,
            Title = statusChanged ? $"Task {FormatStatus(request.Status)}" : "Task Updated",
            Message = notificationMessage,
            EntityId = taskItemId,
            ParentId = request.MilestoneId ?? request.ProjectId
        });

        // Send typed notification when status changed
        if (statusChanged)
        {
            _ = _projectNotificationService.SendTaskStatusChangedAsync(
                _identityContext.UserId,
                new TaskStatusChangedNotification
                {
                    TaskId = taskItemId,
                    Title = request.Title,
                    OldStatus = FormatStatus((Contracts.Models.TaskItemStatus)(int)oldStatus),
                    NewStatus = FormatStatus(request.Status)
                });
        }

        return await GetByIdAsync(taskItemId, cancellationToken: cancellationToken);
    }

    private static string FormatStatus(Contracts.Models.TaskItemStatus status) => status switch
    {
        Contracts.Models.TaskItemStatus.Pending => "Pending",
        Contracts.Models.TaskItemStatus.InProgress => "In Progress",
        Contracts.Models.TaskItemStatus.Completed => "Completed",
        Contracts.Models.TaskItemStatus.Cancelled => "Cancelled",
        _ => status.ToString()
    };

    public async Task<bool> DeleteAsync(Guid taskItemId, CancellationToken cancellationToken = default)
    {
        var userId = _identityContext.UserId;
        var taskItem = await _dbContext.TaskItems
            .FirstOrDefaultAsync(t => t.Id == taskItemId, cancellationToken);

        if (taskItem == null)
        {
            return false;
        }

        var taskItemTitle = taskItem.Title;
        var parentId = taskItem.MilestoneId ?? taskItem.ProjectId;
        _dbContext.TaskItems.Remove(taskItem);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted task item {TaskItemId}", taskItemId);

        // Send notification (fire-and-forget)
        _ = _notificationService.SendAsync(new WorkspaceNotification
        {
            Type = NotificationType.TaskDeleted,
            Title = "Task Deleted",
            Message = $"Task '{taskItemTitle}' has been deleted",
            EntityId = taskItemId,
            ParentId = parentId
        });

        return true;
    }

    private static TaskItemV1 MapToDto(TaskItemEntity taskItem, int? contentOffset = null, int? contentLength = null)
    {
        return new TaskItemV1
        {
            Id = taskItem.Id,
            Title = taskItem.Title,
            Description = ContentTruncationHelper.ApplyChunking(taskItem.Description, contentOffset, contentLength),
            DescriptionLength = ContentTruncationHelper.GetContentLength(taskItem.Description),
            Status = (Contracts.Models.TaskItemStatus)(int)taskItem.Status,
            Priority = (Contracts.Models.TaskItemPriority)(int)taskItem.Priority,
            CompletionNotes = taskItem.CompletionNotes,
            CompletedAt = taskItem.CompletedAt,
            SortOrder = taskItem.SortOrder,
            ProjectId = taskItem.ProjectId,
            MilestoneId = taskItem.MilestoneId,
            Tags = taskItem.Tags.Select(t => new TagV1 { Id = t.Id, Name = t.Name, Color = t.Color }).ToList(),
            CreatedAt = taskItem.CreatedAt,
            UpdatedAt = taskItem.UpdatedAt
        };
    }

    private static TaskItemSummaryV1 MapToSummaryDto(TaskItemEntity taskItem)
    {
        return new TaskItemSummaryV1
        {
            Id = taskItem.Id,
            Title = taskItem.Title,
            Status = (Contracts.Models.TaskItemStatus)(int)taskItem.Status,
            Priority = (Contracts.Models.TaskItemPriority)(int)taskItem.Priority,
            DescriptionPreview = ContentTruncationHelper.TruncateContent(taskItem.Description),
            DescriptionLength = ContentTruncationHelper.GetContentLength(taskItem.Description),
            CompletedAt = taskItem.CompletedAt,
            SortOrder = taskItem.SortOrder,
            ProjectId = taskItem.ProjectId,
            MilestoneId = taskItem.MilestoneId,
            Tags = taskItem.Tags.Select(t => new TagV1 { Id = t.Id, Name = t.Name, Color = t.Color }).ToList(),
            CreatedAt = taskItem.CreatedAt,
            UpdatedAt = taskItem.UpdatedAt
        };
    }
}
