using DonkeyWork.Agents.Projects.Contracts.Models;

namespace DonkeyWork.Agents.Projects.Contracts.Services;

/// <summary>
/// Service for managing milestones.
/// </summary>
public interface IMilestoneService
{
    /// <summary>
    /// Creates a new milestone within a project.
    /// </summary>
    Task<MilestoneDetailsV1?> CreateAsync(Guid projectId, CreateMilestoneRequestV1 request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a milestone by ID with full details.
    /// Supports optional content chunking via offset/length parameters.
    /// </summary>
    Task<MilestoneDetailsV1?> GetByIdAsync(Guid milestoneId, int? contentOffset = null, int? contentLength = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all milestones for a project.
    /// </summary>
    Task<IReadOnlyList<MilestoneSummaryV1>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a milestone.
    /// </summary>
    Task<MilestoneDetailsV1?> UpdateAsync(Guid milestoneId, UpdateMilestoneRequestV1 request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a milestone and all its related data.
    /// </summary>
    Task<bool> DeleteAsync(Guid milestoneId, CancellationToken cancellationToken = default);
}
