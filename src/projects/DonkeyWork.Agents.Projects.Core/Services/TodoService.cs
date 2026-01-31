using DonkeyWork.Agents.Identity.Contracts.Services;
using DonkeyWork.Agents.Persistence;
using DonkeyWork.Agents.Persistence.Entities.Projects;
using DonkeyWork.Agents.Projects.Contracts.Models;
using DonkeyWork.Agents.Projects.Contracts.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DonkeyWork.Agents.Projects.Core.Services;

public class TodoService : ITodoService
{
    private readonly AgentsDbContext _dbContext;
    private readonly IIdentityContext _identityContext;
    private readonly ILogger<TodoService> _logger;

    public TodoService(
        AgentsDbContext dbContext,
        IIdentityContext identityContext,
        ILogger<TodoService> logger)
    {
        _dbContext = dbContext;
        _identityContext = identityContext;
        _logger = logger;
    }

    public async Task<TodoV1> CreateAsync(CreateTodoRequestV1 request, CancellationToken cancellationToken = default)
    {
        var userId = _identityContext.UserId;
        _logger.LogInformation("Creating todo for user {UserId} with title {Title}", userId, request.Title);

        var todoId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var todo = new TodoEntity
        {
            Id = todoId,
            UserId = userId,
            Title = request.Title,
            Description = request.Description,
            Status = (Persistence.Entities.Projects.TodoStatus)(int)request.Status,
            Priority = (Persistence.Entities.Projects.TodoPriority)(int)request.Priority,
            DueDate = request.DueDate,
            SortOrder = request.SortOrder,
            ProjectId = request.ProjectId,
            MilestoneId = request.MilestoneId,
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.Todos.Add(todo);

        // Add tags
        if (request.Tags?.Any() == true)
        {
            foreach (var tag in request.Tags)
            {
                _dbContext.TodoTags.Add(new TodoTagEntity
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    TodoId = todoId,
                    Name = tag.Name,
                    Color = tag.Color,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created todo {TodoId}", todoId);

        return (await GetByIdAsync(todoId, cancellationToken))!;
    }

    public async Task<TodoV1?> GetByIdAsync(Guid todoId, CancellationToken cancellationToken = default)
    {
        var todo = await _dbContext.Todos
            .AsNoTracking()
            .Include(t => t.Tags)
            .FirstOrDefaultAsync(t => t.Id == todoId, cancellationToken);

        return todo == null ? null : MapToDto(todo);
    }

    public async Task<IReadOnlyList<TodoV1>> GetStandaloneAsync(CancellationToken cancellationToken = default)
    {
        var todos = await _dbContext.Todos
            .AsNoTracking()
            .Include(t => t.Tags)
            .Where(t => t.ProjectId == null && t.MilestoneId == null)
            .OrderBy(t => t.SortOrder)
            .ThenByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);

        return todos.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<TodoV1>> ListAsync(CancellationToken cancellationToken = default)
    {
        var todos = await _dbContext.Todos
            .AsNoTracking()
            .Include(t => t.Tags)
            .OrderBy(t => t.SortOrder)
            .ThenByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);

        return todos.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<TodoV1>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        var todos = await _dbContext.Todos
            .AsNoTracking()
            .Include(t => t.Tags)
            .Where(t => t.ProjectId == projectId)
            .OrderBy(t => t.SortOrder)
            .ThenByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);

        return todos.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<TodoV1>> GetByMilestoneIdAsync(Guid milestoneId, CancellationToken cancellationToken = default)
    {
        var todos = await _dbContext.Todos
            .AsNoTracking()
            .Include(t => t.Tags)
            .Where(t => t.MilestoneId == milestoneId)
            .OrderBy(t => t.SortOrder)
            .ThenByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);

        return todos.Select(MapToDto).ToList();
    }

    public async Task<TodoV1?> UpdateAsync(Guid todoId, UpdateTodoRequestV1 request, CancellationToken cancellationToken = default)
    {
        var userId = _identityContext.UserId;
        var todo = await _dbContext.Todos
            .Include(t => t.Tags)
            .FirstOrDefaultAsync(t => t.Id == todoId, cancellationToken);

        if (todo == null)
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        var wasCompleted = todo.Status == Persistence.Entities.Projects.TodoStatus.Completed;
        var isCompleted = request.Status == Contracts.Models.TodoStatus.Completed;

        todo.Title = request.Title;
        todo.Description = request.Description;
        todo.Status = (Persistence.Entities.Projects.TodoStatus)(int)request.Status;
        todo.Priority = (Persistence.Entities.Projects.TodoPriority)(int)request.Priority;
        todo.CompletionNotes = request.CompletionNotes;
        todo.DueDate = request.DueDate;
        todo.SortOrder = request.SortOrder;
        todo.ProjectId = request.ProjectId;
        todo.MilestoneId = request.MilestoneId;
        todo.UpdatedAt = now;

        // Set completed timestamp if status changed to completed
        if (!wasCompleted && isCompleted)
        {
            todo.CompletedAt = now;
        }
        else if (wasCompleted && !isCompleted)
        {
            todo.CompletedAt = null;
        }

        // Update tags - remove existing and add new
        _dbContext.TodoTags.RemoveRange(todo.Tags);
        if (request.Tags?.Any() == true)
        {
            foreach (var tag in request.Tags)
            {
                _dbContext.TodoTags.Add(new TodoTagEntity
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    TodoId = todoId,
                    Name = tag.Name,
                    Color = tag.Color,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated todo {TodoId}", todoId);

        return await GetByIdAsync(todoId, cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid todoId, CancellationToken cancellationToken = default)
    {
        var todo = await _dbContext.Todos
            .FirstOrDefaultAsync(t => t.Id == todoId, cancellationToken);

        if (todo == null)
        {
            return false;
        }

        _dbContext.Todos.Remove(todo);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted todo {TodoId}", todoId);

        return true;
    }

    private static TodoV1 MapToDto(TodoEntity todo)
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
}
