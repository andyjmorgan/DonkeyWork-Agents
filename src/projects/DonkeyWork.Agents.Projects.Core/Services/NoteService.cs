using DonkeyWork.Agents.Persistence;
using DonkeyWork.Agents.Persistence.Entities.Projects;
using DonkeyWork.Agents.Projects.Contracts.Models;
using DonkeyWork.Agents.Projects.Contracts.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Projects.Core.Services;

public class NoteService : INoteService
{
    private readonly AgentsDbContext _dbContext;
    private readonly ILogger<NoteService> _logger;

    public NoteService(AgentsDbContext dbContext, ILogger<NoteService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<NoteV1> CreateAsync(CreateNoteRequestV1 request, Guid userId, CancellationToken cancellationToken = default)
    {
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
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.Notes.Add(note);

        // Add tags
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

        return (await GetByIdAsync(noteId, userId, cancellationToken))!;
    }

    public async Task<NoteV1?> GetByIdAsync(Guid noteId, Guid userId, CancellationToken cancellationToken = default)
    {
        var note = await _dbContext.Notes
            .AsNoTracking()
            .Include(n => n.Tags)
            .FirstOrDefaultAsync(n => n.Id == noteId, cancellationToken);

        return note == null ? null : MapToDto(note);
    }

    public async Task<IReadOnlyList<NoteV1>> GetStandaloneAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var notes = await _dbContext.Notes
            .AsNoTracking()
            .Include(n => n.Tags)
            .Where(n => n.ProjectId == null && n.MilestoneId == null)
            .OrderBy(n => n.SortOrder)
            .ThenByDescending(n => n.CreatedAt)
            .ToListAsync(cancellationToken);

        return notes.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<NoteV1>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var notes = await _dbContext.Notes
            .AsNoTracking()
            .Include(n => n.Tags)
            .OrderBy(n => n.SortOrder)
            .ThenByDescending(n => n.CreatedAt)
            .ToListAsync(cancellationToken);

        return notes.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<NoteV1>> GetByProjectIdAsync(Guid projectId, Guid userId, CancellationToken cancellationToken = default)
    {
        var notes = await _dbContext.Notes
            .AsNoTracking()
            .Include(n => n.Tags)
            .Where(n => n.ProjectId == projectId)
            .OrderBy(n => n.SortOrder)
            .ThenByDescending(n => n.CreatedAt)
            .ToListAsync(cancellationToken);

        return notes.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<NoteV1>> GetByMilestoneIdAsync(Guid milestoneId, Guid userId, CancellationToken cancellationToken = default)
    {
        var notes = await _dbContext.Notes
            .AsNoTracking()
            .Include(n => n.Tags)
            .Where(n => n.MilestoneId == milestoneId)
            .OrderBy(n => n.SortOrder)
            .ThenByDescending(n => n.CreatedAt)
            .ToListAsync(cancellationToken);

        return notes.Select(MapToDto).ToList();
    }

    public async Task<NoteV1?> UpdateAsync(Guid noteId, UpdateNoteRequestV1 request, Guid userId, CancellationToken cancellationToken = default)
    {
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
        note.UpdatedAt = now;

        // Update tags - remove existing and add new
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

        return await GetByIdAsync(noteId, userId, cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid noteId, Guid userId, CancellationToken cancellationToken = default)
    {
        var note = await _dbContext.Notes
            .FirstOrDefaultAsync(n => n.Id == noteId, cancellationToken);

        if (note == null)
        {
            return false;
        }

        _dbContext.Notes.Remove(note);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted note {NoteId}", noteId);

        return true;
    }

    private static NoteV1 MapToDto(NoteEntity note)
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
