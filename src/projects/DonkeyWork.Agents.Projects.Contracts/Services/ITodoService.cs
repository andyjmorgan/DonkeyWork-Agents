using DonkeyWork.Agents.Projects.Contracts.Models;

namespace DonkeyWork.Agents.Projects.Contracts.Services;

/// <summary>
/// Service for managing todos.
/// </summary>
public interface ITodoService
{
    /// <summary>
    /// Creates a new todo (standalone or within a project/milestone).
    /// </summary>
    Task<TodoV1> CreateAsync(CreateTodoRequestV1 request, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a todo by ID.
    /// </summary>
    Task<TodoV1?> GetByIdAsync(Guid todoId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all standalone todos for a user (not associated with any project or milestone).
    /// </summary>
    Task<IReadOnlyList<TodoV1>> GetStandaloneAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all todos for a user.
    /// </summary>
    Task<IReadOnlyList<TodoV1>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all todos for a project.
    /// </summary>
    Task<IReadOnlyList<TodoV1>> GetByProjectIdAsync(Guid projectId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all todos for a milestone.
    /// </summary>
    Task<IReadOnlyList<TodoV1>> GetByMilestoneIdAsync(Guid milestoneId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a todo.
    /// </summary>
    Task<TodoV1?> UpdateAsync(Guid todoId, UpdateTodoRequestV1 request, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a todo.
    /// </summary>
    Task<bool> DeleteAsync(Guid todoId, Guid userId, CancellationToken cancellationToken = default);
}
