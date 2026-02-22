using DonkeyWork.Agents.Projects.Contracts.Models;

namespace DonkeyWork.Agents.Projects.Contracts.Services;

/// <summary>
/// Service for managing research items.
/// </summary>
public interface IResearchService
{
    /// <summary>
    /// Creates a new research item.
    /// </summary>
    Task<ResearchDetailsV1> CreateAsync(CreateResearchRequestV1 request, CancellationToken ct = default);

    /// <summary>
    /// Gets a research item by ID with full details.
    /// Supports optional content chunking via offset/length parameters.
    /// </summary>
    Task<ResearchDetailsV1?> GetByIdAsync(Guid id, int? contentOffset = null, int? contentLength = null, CancellationToken ct = default);

    /// <summary>
    /// Lists all research items for the current user.
    /// </summary>
    Task<IReadOnlyList<ResearchSummaryV1>> ListAsync(CancellationToken ct = default);

    /// <summary>
    /// Updates a research item.
    /// </summary>
    Task<ResearchDetailsV1?> UpdateAsync(Guid id, UpdateResearchRequestV1 request, CancellationToken ct = default);

    /// <summary>
    /// Deletes a research item and all its related data.
    /// </summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}
