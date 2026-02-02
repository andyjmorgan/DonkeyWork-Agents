using DonkeyWork.Agents.Identity.Contracts.Services;
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
    private readonly ILogger<ProjectService> _logger;

    public ProjectService(
        AgentsDbContext dbContext,
        IIdentityContext identityContext,
        ILogger<ProjectService> logger)
    {
        _dbContext = dbContext;
        _identityContext = identityContext;
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
            SuccessCriteria = request.SuccessCriteria,
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
            .Include(p => p.Todos)
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
            .Include(p => p.Todos)
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

        project.Name = request.Name;
        project.Content = request.Content;
        project.SuccessCriteria = request.SuccessCriteria;
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

        return await GetByIdAsync(projectId, cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var project = await _dbContext.Projects
            .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);

        if (project == null)
        {
            return false;
        }

        _dbContext.Projects.Remove(project);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted project {ProjectId}", projectId);

        return true;
    }

    private static ProjectSummaryV1 MapToSummary(ProjectEntity project)
    {
        var allTodos = project.Todos.ToList();
        foreach (var milestone in project.Milestones)
        {
            allTodos.AddRange(milestone.Todos);
        }

        return new ProjectSummaryV1
        {
            Id = project.Id,
            Name = project.Name,
            Status = (Contracts.Models.ProjectStatus)(int)project.Status,
            Tags = project.Tags.Select(t => new TagV1 { Id = t.Id, Name = t.Name, Color = t.Color }).ToList(),
            MilestoneCount = project.Milestones.Count,
            TodoCount = allTodos.Count,
            CompletedTodoCount = allTodos.Count(t => t.Status == Persistence.Entities.Projects.TodoStatus.Completed),
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
            SuccessCriteria = project.SuccessCriteria,
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
                Content = m.Content,
                Status = (Contracts.Models.MilestoneStatus)(int)m.Status,
                DueDate = m.DueDate,
                SortOrder = m.SortOrder,
                Tags = m.Tags.Select(t => new TagV1 { Id = t.Id, Name = t.Name, Color = t.Color }).ToList(),
                TodoCount = m.Todos.Count,
                CompletedTodoCount = m.Todos.Count(t => t.Status == Persistence.Entities.Projects.TodoStatus.Completed),
                CreatedAt = m.CreatedAt,
                UpdatedAt = m.UpdatedAt
            }).ToList(),
            Todos = project.Todos.OrderBy(t => t.SortOrder).Select(MapTodo).ToList(),
            Notes = project.Notes.OrderBy(n => n.SortOrder).Select(MapNote).ToList(),
            CreatedAt = project.CreatedAt,
            UpdatedAt = project.UpdatedAt
        };
    }

    private static TodoV1 MapTodo(TodoEntity todo)
    {
        return new TodoV1
        {
            Id = todo.Id,
            Title = todo.Title,
            Description = todo.Description,
            Status = (Contracts.Models.TodoStatus)(int)todo.Status,
            Priority = (Contracts.Models.TodoPriority)(int)todo.Priority,
            CompletionNotes = todo.CompletionNotes,
            DueDate = todo.DueDate,
            CompletedAt = todo.CompletedAt,
            SortOrder = todo.SortOrder,
            ProjectId = todo.ProjectId,
            MilestoneId = todo.MilestoneId,
            Tags = todo.Tags.Select(t => new TagV1 { Id = t.Id, Name = t.Name, Color = t.Color }).ToList(),
            CreatedAt = todo.CreatedAt,
            UpdatedAt = todo.UpdatedAt
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
