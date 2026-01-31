using DonkeyWork.Agents.Projects.Contracts.Models;

namespace DonkeyWork.Agents.Projects.Contracts.Services;

/// <summary>
/// Service for managing projects.
/// </summary>
public interface IProjectService
{
    /// <summary>
    /// Creates a new project.
    /// </summary>
    Task<ProjectDetailsV1> CreateAsync(CreateProjectRequestV1 request, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a project by ID with full details.
    /// </summary>
    Task<ProjectDetailsV1?> GetByIdAsync(Guid projectId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all projects for a user.
    /// </summary>
    Task<IReadOnlyList<ProjectSummaryV1>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a project.
    /// </summary>
    Task<ProjectDetailsV1?> UpdateAsync(Guid projectId, UpdateProjectRequestV1 request, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a project and all its related data.
    /// </summary>
    Task<bool> DeleteAsync(Guid projectId, Guid userId, CancellationToken cancellationToken = default);
}
