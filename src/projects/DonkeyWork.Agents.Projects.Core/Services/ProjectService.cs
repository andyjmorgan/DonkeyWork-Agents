using DonkeyWork.Agents.Identity.Contracts.Services;
using DonkeyWork.Agents.Notifications.Contracts.Enums;
using DonkeyWork.Agents.Notifications.Contracts.Interfaces;
using DonkeyWork.Agents.Notifications.Contracts.Models;
using DonkeyWork.Agents.Persistence;
using DonkeyWork.Agents.Persistence.Entities.Projects;
using DonkeyWork.Agents.Projects.Contracts.Models;
using DonkeyWork.Agents.Projects.Contracts.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Projects.Core.Services;

public class ProjectService : IProjectService
{
    private readonly AgentsDbContext _dbContext;
    private readonly IIdentityContext _identityContext;
    private readonly INotificationService _notificationService;
    private readonly ILogger<ProjectService> _logger;

    public ProjectService(
        AgentsDbContext dbContext,
        IIdentityContext identityContext,
        INotificationService notificationService,
        ILogger<ProjectService> logger)
    {
        _dbContext = dbContext;
        _identityContext = identityContext;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<ProjectDetailsV1> CreateAsync(CreateProjectRequestV1 request, CancellationToken cancellationToken = default)
    {
        var userId = _identityContext.UserId;
        _logger.LogInformation("Creating project for user {UserId} with name {Name}", userId, request.Name);

        var projectId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var project = new ProjectEntity
        {
            Id = projectId,
            UserId = userId,
            Name = request.Name,
            Content = request.Content,
            Status = (Persistence.Entities.Projects.ProjectStatus)(int)request.Status,
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.Projects.Add(project);

        // Add tags
        if (request.Tags?.Any() == true)
        {
            foreach (var tag in request.Tags)
            {
                _dbContext.ProjectTags.Add(new ProjectTagEntity
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    ProjectId = projectId,
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
                _dbContext.ProjectFileReferences.Add(new ProjectFileReferenceEntity
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    ProjectId = projectId,
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

        _logger.LogInformation("Created project {ProjectId}", projectId);

        // Send notification (fire-and-forget)
        _ = _notificationService.SendAsync(new WorkspaceNotification
        {
            Type = NotificationType.ProjectCreated,
            Title = "Project Created",
            Message = $"Project '{request.Name}' has been created",
            EntityId = projectId
        });

        return (await GetByIdAsync(projectId, cancellationToken))!;
    }

    public async Task<ProjectDetailsV1?> GetByIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var project = await _dbContext.Projects
            .AsNoTracking()
            .Include(p => p.Tags)
            .Include(p => p.FileReferences)
            .Include(p => p.Milestones)
                .ThenInclude(m => m.Tags)
            .Include(p => p.TaskItems)
                .ThenInclude(t => t.Tags)
            .Include(p => p.Notes)
                .ThenInclude(n => n.Tags)
            .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);

        return project == null ? null : MapToDetails(project);
    }

    public async Task<IReadOnlyList<ProjectSummaryV1>> ListAsync(CancellationToken cancellationToken = default)
    {
        var projects = await _dbContext.Projects
            .AsNoTracking()
            .Include(p => p.Tags)
            .Include(p => p.Milestones)
            .Include(p => p.TaskItems)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

        return projects.Select(MapToSummary).ToList();
    }

    public async Task<ProjectDetailsV1?> UpdateAsync(Guid projectId, UpdateProjectRequestV1 request, CancellationToken cancellationToken = default)
    {
        var userId = _identityContext.UserId;
        var project = await _dbContext.Projects
            .Include(p => p.Tags)
            .Include(p => p.FileReferences)
            .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);

        if (project == null)
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        var oldStatus = project.Status;

        project.Name = request.Name;
        project.Content = request.Content;
        project.Status = (Persistence.Entities.Projects.ProjectStatus)(int)request.Status;
        project.UpdatedAt = now;

        // Update tags - remove existing and add new
        _dbContext.ProjectTags.RemoveRange(project.Tags);
        if (request.Tags?.Any() == true)
        {
            foreach (var tag in request.Tags)
            {
                _dbContext.ProjectTags.Add(new ProjectTagEntity
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    ProjectId = projectId,
                    Name = tag.Name,
                    Color = tag.Color,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }
        }

        // Update file references - remove existing and add new
        _dbContext.ProjectFileReferences.RemoveRange(project.FileReferences);
        if (request.FileReferences?.Any() == true)
        {
            foreach (var fileRef in request.FileReferences)
            {
                _dbContext.ProjectFileReferences.Add(new ProjectFileReferenceEntity
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    ProjectId = projectId,
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

        _logger.LogInformation("Updated project {ProjectId}", projectId);

        // Send notification (fire-and-forget)
        var newStatus = (Persistence.Entities.Projects.ProjectStatus)(int)request.Status;
        var statusChanged = oldStatus != newStatus;
        var notificationMessage = statusChanged
            ? $"'{request.Name}' is now {FormatStatus(request.Status)}"
            : $"'{request.Name}' has been updated";

        _ = _notificationService.SendAsync(new WorkspaceNotification
        {
            Type = NotificationType.ProjectUpdated,
            Title = statusChanged ? $"Project {FormatStatus(request.Status)}" : "Project Updated",
            Message = notificationMessage,
            EntityId = projectId
        });

        return await GetByIdAsync(projectId, cancellationToken);
    }

    private static string FormatStatus(Contracts.Models.ProjectStatus status) => status switch
    {
        Contracts.Models.ProjectStatus.NotStarted => "Not Started",
        Contracts.Models.ProjectStatus.InProgress => "In Progress",
        Contracts.Models.ProjectStatus.OnHold => "On Hold",
        Contracts.Models.ProjectStatus.Completed => "Completed",
        Contracts.Models.ProjectStatus.Cancelled => "Cancelled",
        _ => status.ToString()
    };

    public async Task<bool> DeleteAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var userId = _identityContext.UserId;
        var project = await _dbContext.Projects
            .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);

        if (project == null)
        {
            return false;
        }

        var projectName = project.Name;
        _dbContext.Projects.Remove(project);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted project {ProjectId}", projectId);

        // Send notification (fire-and-forget)
        _ = _notificationService.SendAsync(new WorkspaceNotification
        {
            Type = NotificationType.ProjectDeleted,
            Title = "Project Deleted",
            Message = $"Project '{projectName}' has been deleted",
            EntityId = projectId
        });

        return true;
    }

    private static ProjectSummaryV1 MapToSummary(ProjectEntity project)
    {
        var allTaskItems = project.TaskItems.ToList();
        foreach (var milestone in project.Milestones)
        {
            allTaskItems.AddRange(milestone.TaskItems);
        }

        return new ProjectSummaryV1
        {
            Id = project.Id,
            Name = project.Name,
            Status = (Contracts.Models.ProjectStatus)(int)project.Status,
            Tags = project.Tags.Select(t => new TagV1 { Id = t.Id, Name = t.Name, Color = t.Color }).ToList(),
            MilestoneCount = project.Milestones.Count,
            TaskItemCount = allTaskItems.Count,
            CompletedTaskItemCount = allTaskItems.Count(t => t.Status == Persistence.Entities.Projects.TaskItemStatus.Completed),
            CreatedAt = project.CreatedAt,
            UpdatedAt = project.UpdatedAt
        };
    }

    private static ProjectDetailsV1 MapToDetails(ProjectEntity project)
    {
        return new ProjectDetailsV1
        {
            Id = project.Id,
            Name = project.Name,
            Content = project.Content,
            Status = (Contracts.Models.ProjectStatus)(int)project.Status,
            Tags = project.Tags.Select(t => new TagV1 { Id = t.Id, Name = t.Name, Color = t.Color }).ToList(),
            FileReferences = project.FileReferences.OrderBy(f => f.SortOrder).Select(f => new FileReferenceV1
            {
                Id = f.Id,
                FilePath = f.FilePath,
                DisplayName = f.DisplayName,
                Description = f.Description,
                SortOrder = f.SortOrder
            }).ToList(),
            Milestones = project.Milestones.OrderBy(m => m.SortOrder).Select(m => new MilestoneSummaryV1
            {
                Id = m.Id,
                ProjectId = m.ProjectId,
                Name = m.Name,
                Status = (Contracts.Models.MilestoneStatus)(int)m.Status,
                DueDate = m.DueDate,
                SortOrder = m.SortOrder,
                Tags = m.Tags.Select(t => new TagV1 { Id = t.Id, Name = t.Name, Color = t.Color }).ToList(),
                TaskItemCount = m.TaskItems.Count,
                CompletedTaskItemCount = m.TaskItems.Count(t => t.Status == Persistence.Entities.Projects.TaskItemStatus.Completed),
                CreatedAt = m.CreatedAt,
                UpdatedAt = m.UpdatedAt
            }).ToList(),
            Tasks = project.TaskItems.OrderBy(t => t.SortOrder).Select(MapTaskItem).ToList(),
            Notes = project.Notes.OrderBy(n => n.SortOrder).Select(MapNote).ToList(),
            CreatedAt = project.CreatedAt,
            UpdatedAt = project.UpdatedAt
        };
    }

    private static TaskItemV1 MapTaskItem(TaskItemEntity taskItem)
    {
        return new TaskItemV1
        {
            Id = taskItem.Id,
            Title = taskItem.Title,
            Description = taskItem.Description,
            Status = (Contracts.Models.TaskItemStatus)(int)taskItem.Status,
            Priority = (Contracts.Models.TaskItemPriority)(int)taskItem.Priority,
            CompletionNotes = taskItem.CompletionNotes,
            DueDate = taskItem.DueDate,
            CompletedAt = taskItem.CompletedAt,
            SortOrder = taskItem.SortOrder,
            ProjectId = taskItem.ProjectId,
            MilestoneId = taskItem.MilestoneId,
            Tags = taskItem.Tags.Select(t => new TagV1 { Id = t.Id, Name = t.Name, Color = t.Color }).ToList(),
            CreatedAt = taskItem.CreatedAt,
            UpdatedAt = taskItem.UpdatedAt
        };
    }

    private static NoteV1 MapNote(NoteEntity note)
    {
        return new NoteV1
        {
            Id = note.Id,
            Title = note.Title,
            Content = note.Content,
            SortOrder = note.SortOrder,
            ProjectId = note.ProjectId,
            MilestoneId = note.MilestoneId,
            Tags = note.Tags.Select(t => new TagV1 { Id = t.Id, Name = t.Name, Color = t.Color }).ToList(),
            CreatedAt = note.CreatedAt,
            UpdatedAt = note.UpdatedAt
        };
    }
}
