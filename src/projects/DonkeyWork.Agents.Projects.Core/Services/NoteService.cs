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

public class NoteService : INoteService
{
    private readonly AgentsDbContext _dbContext;
    private readonly IIdentityContext _identityContext;
    private readonly INotificationService _notificationService;
    private readonly IProjectNotificationService _projectNotificationService;
    private readonly ILogger<NoteService> _logger;

    public NoteService(
        AgentsDbContext dbContext,
        IIdentityContext identityContext,
        INotificationService notificationService,
        IProjectNotificationService projectNotificationService,
        ILogger<NoteService> logger)
    {
        _dbContext = dbContext;
        _identityContext = identityContext;
        _notificationService = notificationService;
        _projectNotificationService = projectNotificationService;
        _logger = logger;
    }

    public async Task<NoteV1> CreateAsync(CreateNoteRequestV1 request, CancellationToken cancellationToken = default)
    {
        var userId = _identityContext.UserId;
        _logger.LogInformation("Creating note for user {UserId} with title {Title}", userId, request.Title);

        var noteId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var note = new NoteEntity
        {
            Id = noteId,
            UserId = userId,
            Title = request.Title,
            Content = request.Content,
            SortOrder = request.SortOrder,
            ProjectId = request.ProjectId,
            MilestoneId = request.MilestoneId,
            ResearchId = request.ResearchId,
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.Notes.Add(note);

        if (request.Tags?.Any() == true)
        {
            foreach (var tag in request.Tags)
            {
                _dbContext.NoteTags.Add(new NoteTagEntity
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    NoteId = noteId,
                    Name = tag.Name,
                    Color = tag.Color,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created note {NoteId}", noteId);

        _ = _notificationService.SendAsync(new WorkspaceNotification
        {
            Type = NotificationType.NoteCreated,
            Title = "Note Created",
            Message = $"Note '{request.Title}' has been created",
            EntityId = noteId,
            ParentId = request.MilestoneId ?? request.ProjectId
        });

        return (await GetByIdAsync(noteId, cancellationToken: cancellationToken))!;
    }

    public async Task<NoteV1?> GetByIdAsync(Guid noteId, int? contentOffset = null, int? contentLength = null, CancellationToken cancellationToken = default)
    {
        var note = await _dbContext.Notes
            .AsNoTracking()
            .Include(n => n.Tags)
            .FirstOrDefaultAsync(n => n.Id == noteId, cancellationToken);

        return note == null ? null : MapToDto(note, contentOffset, contentLength);
    }

    public async Task<IReadOnlyList<NoteSummaryV1>> GetStandaloneAsync(CancellationToken cancellationToken = default)
    {
        var notes = await _dbContext.Notes
            .AsNoTracking()
            .Include(n => n.Tags)
            .Where(n => n.ProjectId == null && n.MilestoneId == null && n.ResearchId == null)
            .OrderBy(n => n.SortOrder)
            .ThenByDescending(n => n.CreatedAt)
            .ToListAsync(cancellationToken);

        return notes.Select(MapToSummaryDto).ToList();
    }

    public async Task<IReadOnlyList<NoteSummaryV1>> ListAsync(CancellationToken cancellationToken = default)
    {
        var notes = await _dbContext.Notes
            .AsNoTracking()
            .Include(n => n.Tags)
            .OrderBy(n => n.SortOrder)
            .ThenByDescending(n => n.CreatedAt)
            .ToListAsync(cancellationToken);

        return notes.Select(MapToSummaryDto).ToList();
    }

    public async Task<IReadOnlyList<NoteSummaryV1>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var notes = await _dbContext.Notes
            .AsNoTracking()
            .Include(n => n.Tags)
            .Where(n => n.ProjectId == projectId)
            .OrderBy(n => n.SortOrder)
            .ThenByDescending(n => n.CreatedAt)
            .ToListAsync(cancellationToken);

        return notes.Select(MapToSummaryDto).ToList();
    }

    public async Task<IReadOnlyList<NoteSummaryV1>> GetByMilestoneIdAsync(Guid milestoneId, CancellationToken cancellationToken = default)
    {
        var notes = await _dbContext.Notes
            .AsNoTracking()
            .Include(n => n.Tags)
            .Where(n => n.MilestoneId == milestoneId)
            .OrderBy(n => n.SortOrder)
            .ThenByDescending(n => n.CreatedAt)
            .ToListAsync(cancellationToken);

        return notes.Select(MapToSummaryDto).ToList();
    }

    public async Task<IReadOnlyList<NoteSummaryV1>> GetByResearchIdAsync(Guid researchId, CancellationToken cancellationToken = default)
    {
        var notes = await _dbContext.Notes
            .AsNoTracking()
            .Include(n => n.Tags)
            .Where(n => n.ResearchId == researchId)
            .OrderBy(n => n.SortOrder)
            .ThenByDescending(n => n.CreatedAt)
            .ToListAsync(cancellationToken);

        return notes.Select(MapToSummaryDto).ToList();
    }

    public async Task<NoteV1?> UpdateAsync(Guid noteId, UpdateNoteRequestV1 request, CancellationToken cancellationToken = default)
    {
        var userId = _identityContext.UserId;
        var note = await _dbContext.Notes
            .Include(n => n.Tags)
            .FirstOrDefaultAsync(n => n.Id == noteId, cancellationToken);

        if (note == null)
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;

        note.Title = request.Title;
        note.Content = request.Content;
        note.SortOrder = request.SortOrder;
        note.ProjectId = request.ProjectId;
        note.MilestoneId = request.MilestoneId;
        note.ResearchId = request.ResearchId;
        note.UpdatedAt = now;

        _dbContext.NoteTags.RemoveRange(note.Tags);
        if (request.Tags?.Any() == true)
        {
            foreach (var tag in request.Tags)
            {
                _dbContext.NoteTags.Add(new NoteTagEntity
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    NoteId = noteId,
                    Name = tag.Name,
                    Color = tag.Color,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated note {NoteId}", noteId);

        _ = _notificationService.SendAsync(new WorkspaceNotification
        {
            Type = NotificationType.NoteUpdated,
            Title = "Note Updated",
            Message = $"Note '{request.Title}' has been updated",
            EntityId = noteId,
            ParentId = request.MilestoneId ?? request.ProjectId
        });

        _ = _projectNotificationService.SendNoteUpdatedAsync(
            _identityContext.UserId,
            new NoteUpdatedNotification
            {
                NoteId = noteId,
                Title = request.Title
            });

        return await GetByIdAsync(noteId, cancellationToken: cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid noteId, CancellationToken cancellationToken = default)
    {
        var userId = _identityContext.UserId;
        var note = await _dbContext.Notes
            .FirstOrDefaultAsync(n => n.Id == noteId, cancellationToken);

        if (note == null)
        {
            return false;
        }

        var noteTitle = note.Title;
        var parentId = note.MilestoneId ?? note.ProjectId;
        _dbContext.Notes.Remove(note);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted note {NoteId}", noteId);

        _ = _notificationService.SendAsync(new WorkspaceNotification
        {
            Type = NotificationType.NoteDeleted,
            Title = "Note Deleted",
            Message = $"Note '{noteTitle}' has been deleted",
            EntityId = noteId,
            ParentId = parentId
        });

        return true;
    }

    private static NoteV1 MapToDto(NoteEntity note, int? contentOffset = null, int? contentLength = null)
    {
        return new NoteV1
        {
            Id = note.Id,
            Title = note.Title,
            Content = ContentTruncationHelper.ApplyChunking(note.Content, contentOffset, contentLength),
            ContentLength = ContentTruncationHelper.GetContentLength(note.Content),
            SortOrder = note.SortOrder,
            ProjectId = note.ProjectId,
            MilestoneId = note.MilestoneId,
            ResearchId = note.ResearchId,
            Tags = note.Tags.Select(t => new TagV1 { Id = t.Id, Name = t.Name, Color = t.Color }).ToList(),
            CreatedAt = note.CreatedAt,
            UpdatedAt = note.UpdatedAt
        };
    }

    private static NoteSummaryV1 MapToSummaryDto(NoteEntity note)
    {
        return new NoteSummaryV1
        {
            Id = note.Id,
            Title = note.Title,
            ContentPreview = ContentTruncationHelper.TruncateContent(note.Content),
            ContentLength = ContentTruncationHelper.GetContentLength(note.Content),
            SortOrder = note.SortOrder,
            ProjectId = note.ProjectId,
            MilestoneId = note.MilestoneId,
            ResearchId = note.ResearchId,
            Tags = note.Tags.Select(t => new TagV1 { Id = t.Id, Name = t.Name, Color = t.Color }).ToList(),
            CreatedAt = note.CreatedAt,
            UpdatedAt = note.UpdatedAt
        };
    }
}
