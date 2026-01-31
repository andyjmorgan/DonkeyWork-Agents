using DonkeyWork.Agents.Persistence;
using DonkeyWork.Agents.Persistence.Entities.Projects;
using DonkeyWork.Agents.Projects.Contracts.Models;
using DonkeyWork.Agents.Projects.Contracts.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Projects.Core.Services;

public class MilestoneService : IMilestoneService
{
    private readonly AgentsDbContext _dbContext;
    private readonly ILogger<MilestoneService> _logger;

    public MilestoneService(AgentsDbContext dbContext, ILogger<MilestoneService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<MilestoneDetailsV1?> CreateAsync(Guid projectId, CreateMilestoneRequestV1 request, Guid userId, CancellationToken cancellationToken = default)
    {
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
            Description = request.Description,
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

        return await GetByIdAsync(milestoneId, userId, cancellationToken);
    }

    public async Task<MilestoneDetailsV1?> GetByIdAsync(Guid milestoneId, Guid userId, CancellationToken cancellationToken = default)
    {
        var milestone = await _dbContext.Milestones
            .AsNoTracking()
            .Include(m => m.Tags)
            .Include(m => m.FileReferences)
            .Include(m => m.Todos)
                .ThenInclude(t => t.Tags)
            .Include(m => m.Notes)
                .ThenInclude(n => n.Tags)
            .FirstOrDefaultAsync(m => m.Id == milestoneId, cancellationToken);

        return milestone == null ? null : MapToDetails(milestone);
    }

    public async Task<IReadOnlyList<MilestoneSummaryV1>> GetByProjectIdAsync(Guid projectId, Guid userId, CancellationToken cancellationToken = default)
    {
        var milestones = await _dbContext.Milestones
            .AsNoTracking()
            .Include(m => m.Tags)
            .Include(m => m.Todos)
            .Where(m => m.ProjectId == projectId)
            .OrderBy(m => m.SortOrder)
            .ToListAsync(cancellationToken);

        return milestones.Select(MapToSummary).ToList();
    }

    public async Task<MilestoneDetailsV1?> UpdateAsync(Guid milestoneId, UpdateMilestoneRequestV1 request, Guid userId, CancellationToken cancellationToken = default)
    {
        var milestone = await _dbContext.Milestones
            .Include(m => m.Tags)
            .Include(m => m.FileReferences)
            .FirstOrDefaultAsync(m => m.Id == milestoneId, cancellationToken);

        if (milestone == null)
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;

        milestone.Name = request.Name;
        milestone.Description = request.Description;
        milestone.SuccessCriteria = request.SuccessCriteria;
        milestone.Status = (Persistence.Entities.Projects.MilestoneStatus)(int)request.Status;
        milestone.DueDate = request.DueDate;
        milestone.SortOrder = request.SortOrder;
        milestone.UpdatedAt = now;

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

        return await GetByIdAsync(milestoneId, userId, cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid milestoneId, Guid userId, CancellationToken cancellationToken = default)
    {
        var milestone = await _dbContext.Milestones
            .FirstOrDefaultAsync(m => m.Id == milestoneId, cancellationToken);

        if (milestone == null)
        {
            return false;
        }

        _dbContext.Milestones.Remove(milestone);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted milestone {MilestoneId}", milestoneId);

        return true;
    }

    private static MilestoneSummaryV1 MapToSummary(MilestoneEntity milestone)
    {
        return new MilestoneSummaryV1
        {
            Id = milestone.Id,
            ProjectId = milestone.ProjectId,
            Name = milestone.Name,
            Description = milestone.Description,
            Status = (Contracts.Models.MilestoneStatus)(int)milestone.Status,
            DueDate = milestone.DueDate,
            SortOrder = milestone.SortOrder,
            Tags = milestone.Tags.Select(t => new TagV1 { Id = t.Id, Name = t.Name, Color = t.Color }).ToList(),
            TodoCount = milestone.Todos.Count,
            CompletedTodoCount = milestone.Todos.Count(t => t.Status == Persistence.Entities.Projects.TodoStatus.Completed),
            CreatedAt = milestone.CreatedAt,
            UpdatedAt = milestone.UpdatedAt
        };
    }

    private static MilestoneDetailsV1 MapToDetails(MilestoneEntity milestone)
    {
        return new MilestoneDetailsV1
        {
            Id = milestone.Id,
            ProjectId = milestone.ProjectId,
            Name = milestone.Name,
            Description = milestone.Description,
            SuccessCriteria = milestone.SuccessCriteria,
            Status = (Contracts.Models.MilestoneStatus)(int)milestone.Status,
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
            Todos = milestone.Todos.OrderBy(t => t.SortOrder).Select(t => new TodoV1
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                Status = (Contracts.Models.TodoStatus)(int)t.Status,
                Priority = (Contracts.Models.TodoPriority)(int)t.Priority,
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
