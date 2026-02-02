using DonkeyWork.Agents.Orchestrations.Contracts.Models;

namespace DonkeyWork.Agents.Orchestrations.Contracts.Services;

/// <summary>
/// Service for managing agent versions.
/// </summary>
public interface IOrchestrationVersionService
{
    /// <summary>
    /// Saves a draft version (creates new or updates existing draft).
    /// </summary>
    Task<GetOrchestrationVersionResponseV1> SaveDraftAsync(Guid orchestrationId, SaveOrchestrationVersionRequestV1 request, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes the current draft version.
    /// </summary>
    Task<GetOrchestrationVersionResponseV1> PublishAsync(Guid orchestrationId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific version by ID.
    /// </summary>
    Task<GetOrchestrationVersionResponseV1?> GetVersionAsync(Guid orchestrationId, Guid versionId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all versions for an orchestration.
    /// </summary>
    Task<IReadOnlyList<GetOrchestrationVersionResponseV1>> GetVersionsAsync(Guid orchestrationId, Guid userId, CancellationToken cancellationToken = default);
}
