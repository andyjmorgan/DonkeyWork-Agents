using DonkeyWork.Agents.Identity.Contracts.Services;
using DonkeyWork.Agents.Notifications.Contracts.Enums;
using DonkeyWork.Agents.Notifications.Contracts.Interfaces;
using DonkeyWork.Agents.Notifications.Contracts.Models;
using DonkeyWork.Agents.Persistence;
using DonkeyWork.Agents.Persistence.Entities.Research;
using DonkeyWork.Agents.Projects.Contracts.Helpers;
using DonkeyWork.Agents.Projects.Contracts.Models;
using DonkeyWork.Agents.Projects.Contracts.Models.Notifications;
using EntityResearchStatus = DonkeyWork.Agents.Persistence.Entities.Research.ResearchStatus;
using DonkeyWork.Agents.Projects.Contracts.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Projects.Core.Services;

public class ResearchService : IResearchService
{
    private readonly AgentsDbContext _dbContext;
    private readonly IIdentityContext _identityContext;
    private readonly INotificationService _notificationService;
    private readonly IProjectNotificationService _projectNotificationService;
    private readonly ILogger<ResearchService> _logger;

    public ResearchService(
        AgentsDbContext dbContext,
        IIdentityContext identityContext,
        INotificationService notificationService,
        IProjectNotificationService projectNotificationService,
        ILogger<ResearchService> logger)
    {
        _dbContext = dbContext;
        _identityContext = identityContext;
        _notificationService = notificationService;
        _projectNotificationService = projectNotificationService;
        _logger = logger;
    }

    public async Task<ResearchDetailsV1> CreateAsync(CreateResearchRequestV1 request, CancellationToken ct = default)
    {
        var userId = _identityContext.UserId;
        _logger.LogInformation("Creating research for user {UserId} with title {Title}", userId, request.Title);

        var researchId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var research = new ResearchEntity
        {
            Id = researchId,
            UserId = userId,
            Title = request.Title,
            Plan = request.Plan,
            Status = (EntityResearchStatus)(int)request.Status,
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.Research.Add(research);

        // Add tags
        if (request.Tags?.Any() == true)
        {
            foreach (var tag in request.Tags)
            {
                _dbContext.ResearchTags.Add(new ResearchTagEntity
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    ResearchId = researchId,
                    Name = tag.Name,
                    Color = tag.Color,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }
        }

        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("Created research {ResearchId}", researchId);

        // Send notification (fire-and-forget)
        _ = _notificationService.SendAsync(new WorkspaceNotification
        {
            Type = NotificationType.ProjectCreated,
            Title = "Research Created",
            Message = $"Research '{request.Title}' has been created",
            EntityId = researchId
        });

        return (await GetByIdAsync(researchId, ct: ct))!;
    }

    public async Task<ResearchDetailsV1?> GetByIdAsync(Guid id, int? contentOffset = null, int? contentLength = null, CancellationToken ct = default)
    {
        var research = await _dbContext.Research
            .AsNoTracking()
            .Include(r => r.Tags)
            .Include(r => r.Notes)
                .ThenInclude(n => n.Tags)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

        return research == null ? null : MapToDetails(research, contentOffset, contentLength);
    }

    public async Task<IReadOnlyList<ResearchSummaryV1>> ListAsync(CancellationToken ct = default)
    {
        var research = await _dbContext.Research
            .AsNoTracking()
            .Include(r => r.Tags)
            .Include(r => r.Notes)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);

        return research.Select(MapToSummary).ToList();
    }

    public async Task<ResearchDetailsV1?> UpdateAsync(Guid id, UpdateResearchRequestV1 request, CancellationToken ct = default)
    {
        var userId = _identityContext.UserId;
        var research = await _dbContext.Research
            .Include(r => r.Tags)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

        if (research == null)
        {
            return null;
        }

        // Validate completion requirements
        var isCompleted = request.Status is Contracts.Models.ResearchStatus.Completed;
        var isCancelled = request.Status is Contracts.Models.ResearchStatus.Cancelled;

        if (isCompleted && string.IsNullOrWhiteSpace(request.Result))
        {
            throw new InvalidOperationException("Result is required when marking research as completed.");
        }

        var now = DateTimeOffset.UtcNow;
        var oldStatus = research.Status;
        var newStatus = (EntityResearchStatus)(int)request.Status;
        var wasTerminal = oldStatus is EntityResearchStatus.Completed or EntityResearchStatus.Cancelled;
        var isNowTerminal = newStatus is EntityResearchStatus.Completed or EntityResearchStatus.Cancelled;

        research.Title = request.Title;
        research.Plan = request.Plan;
        research.Result = request.Result;
        research.Status = newStatus;
        research.UpdatedAt = now;

        // Set completed timestamp when moving to terminal status
        if (!wasTerminal && isNowTerminal)
        {
            research.CompletedAt = now;
        }
        else if (wasTerminal && !isNowTerminal)
        {
            research.CompletedAt = null;
        }

        // Update tags - remove existing and add new
        _dbContext.ResearchTags.RemoveRange(research.Tags);
        if (request.Tags?.Any() == true)
        {
            foreach (var tag in request.Tags)
            {
                _dbContext.ResearchTags.Add(new ResearchTagEntity
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    ResearchId = id,
                    Name = tag.Name,
                    Color = tag.Color,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }
        }

        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("Updated research {ResearchId}", id);

        // Send notification (fire-and-forget)
        var statusChanged = oldStatus != newStatus;
        var notificationMessage = statusChanged
            ? $"'{request.Title}' is now {FormatStatus(request.Status)}"
            : $"'{request.Title}' has been updated";

        _ = _notificationService.SendAsync(new WorkspaceNotification
        {
            Type = NotificationType.ProjectUpdated,
            Title = statusChanged ? $"Research {FormatStatus(request.Status)}" : "Research Updated",
            Message = notificationMessage,
            EntityId = id
        });

        // Send typed notification when status changed
        if (statusChanged)
        {
            _ = _projectNotificationService.SendResearchStatusChangedAsync(
                _identityContext.UserId,
                new ResearchStatusChangedNotification
                {
                    ResearchId = id,
                    Title = request.Title,
                    Status = FormatStatus(request.Status)
                });
        }

        return await GetByIdAsync(id, ct: ct);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var research = await _dbContext.Research
            .FirstOrDefaultAsync(r => r.Id == id, ct);

        if (research == null)
        {
            return false;
        }

        var title = research.Title;
        _dbContext.Research.Remove(research);
        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("Deleted research {ResearchId}", id);

        // Send notification (fire-and-forget)
        _ = _notificationService.SendAsync(new WorkspaceNotification
        {
            Type = NotificationType.ProjectDeleted,
            Title = "Research Deleted",
            Message = $"Research '{title}' has been deleted",
            EntityId = id
        });

        return true;
    }

    private static string FormatStatus(Contracts.Models.ResearchStatus status) => status switch
    {
        Contracts.Models.ResearchStatus.NotStarted => "Not Started",
        Contracts.Models.ResearchStatus.InProgress => "In Progress",
        Contracts.Models.ResearchStatus.Completed => "Completed",
        Contracts.Models.ResearchStatus.Cancelled => "Cancelled",
        _ => status.ToString()
    };

    private static ResearchSummaryV1 MapToSummary(ResearchEntity research)
    {
        return new ResearchSummaryV1
        {
            Id = research.Id,
            Title = research.Title,
            PlanPreview = ContentTruncationHelper.TruncateContent(research.Plan),
            PlanLength = ContentTruncationHelper.GetContentLength(research.Plan),
            Status = (Contracts.Models.ResearchStatus)(int)research.Status,
            CompletedAt = research.CompletedAt,
            Tags = research.Tags.Select(t => new TagV1 { Id = t.Id, Name = t.Name, Color = t.Color }).ToList(),
            NoteCount = research.Notes.Count,
            CreatedAt = research.CreatedAt,
            UpdatedAt = research.UpdatedAt
        };
    }

    private static ResearchDetailsV1 MapToDetails(ResearchEntity research, int? contentOffset = null, int? contentLength = null)
    {
        return new ResearchDetailsV1
        {
            Id = research.Id,
            Title = research.Title,
            Plan = ContentTruncationHelper.ApplyChunking(research.Plan, contentOffset, contentLength),
            PlanLength = ContentTruncationHelper.GetContentLength(research.Plan),
            Result = ContentTruncationHelper.ApplyChunking(research.Result, contentOffset, contentLength),
            ResultLength = ContentTruncationHelper.GetContentLength(research.Result),
            Status = (Contracts.Models.ResearchStatus)(int)research.Status,
            CompletedAt = research.CompletedAt,
            Tags = research.Tags.Select(t => new TagV1 { Id = t.Id, Name = t.Name, Color = t.Color }).ToList(),
            Notes = research.Notes.OrderBy(n => n.SortOrder).Select(n => new NoteSummaryV1
            {
                Id = n.Id,
                Title = n.Title,
                ContentPreview = ContentTruncationHelper.TruncateContent(n.Content),
                ContentLength = ContentTruncationHelper.GetContentLength(n.Content),
                SortOrder = n.SortOrder,
                ProjectId = n.ProjectId,
                MilestoneId = n.MilestoneId,
                ResearchId = n.ResearchId,
                Tags = n.Tags.Select(t => new TagV1 { Id = t.Id, Name = t.Name, Color = t.Color }).ToList(),
                CreatedAt = n.CreatedAt,
                UpdatedAt = n.UpdatedAt
            }).ToList(),
            CreatedAt = research.CreatedAt,
            UpdatedAt = research.UpdatedAt
        };
    }
}
