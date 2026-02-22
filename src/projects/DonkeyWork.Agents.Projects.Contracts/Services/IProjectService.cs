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
    Task<ProjectDetailsV1> CreateAsync(CreateProjectRequestV1 request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a project by ID with full details.
    /// Supports optional content chunking via offset/length parameters.
    /// </summary>
    Task<ProjectDetailsV1?> GetByIdAsync(Guid projectId, int? contentOffset = null, int? contentLength = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all projects for the current user.
    /// </summary>
    Task<IReadOnlyList<ProjectSummaryV1>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a project.
    /// </summary>
    Task<ProjectDetailsV1?> UpdateAsync(Guid projectId, UpdateProjectRequestV1 request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a project and all its related data.
    /// </summary>
    Task<bool> DeleteAsync(Guid projectId, CancellationToken cancellationToken = default);
}
