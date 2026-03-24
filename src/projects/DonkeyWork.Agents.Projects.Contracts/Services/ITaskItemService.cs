using DonkeyWork.Agents.Common.Contracts.Models.Pagination;
using DonkeyWork.Agents.Projects.Contracts.Models;

namespace DonkeyWork.Agents.Projects.Contracts.Services;

/// <summary>
/// Service for managing task items.
/// </summary>
public interface ITaskItemService
{
    /// <summary>
    /// Creates a new task item (standalone or within a project/milestone).
    /// </summary>
    Task<TaskItemV1> CreateAsync(CreateTaskItemRequestV1 request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a task item by ID.
    /// Supports optional content chunking via offset/length parameters.
    /// </summary>
    Task<TaskItemV1?> GetByIdAsync(Guid taskItemId, int? contentOffset = null, int? contentLength = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all standalone task items for the current user (not associated with any project or milestone).
    /// Returns summary models without description/completionNotes - use GetByIdAsync for full details.
    /// </summary>
    Task<IReadOnlyList<TaskItemSummaryV1>> GetStandaloneAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all task items for the current user with optional filtering and pagination.
    /// Returns summary models without description/completionNotes - use GetByIdAsync for full details.
    /// </summary>
    Task<PaginatedResponse<TaskItemSummaryV1>> ListAsync(PaginationRequest pagination, TaskItemFilterRequestV1? filter = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all task items for a project with optional filtering and pagination.
    /// Returns summary models without description/completionNotes - use GetByIdAsync for full details.
    /// </summary>
    Task<PaginatedResponse<TaskItemSummaryV1>> GetByProjectIdAsync(Guid projectId, PaginationRequest pagination, TaskItemFilterRequestV1? filter = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all task items for a milestone with optional filtering and pagination.
    /// Returns summary models without description/completionNotes - use GetByIdAsync for full details.
    /// </summary>
    Task<PaginatedResponse<TaskItemSummaryV1>> GetByMilestoneIdAsync(Guid milestoneId, PaginationRequest pagination, TaskItemFilterRequestV1? filter = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a task item.
    /// </summary>
    Task<TaskItemV1?> UpdateAsync(Guid taskItemId, UpdateTaskItemRequestV1 request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a task item.
    /// </summary>
    Task<bool> DeleteAsync(Guid taskItemId, CancellationToken cancellationToken = default);
}
