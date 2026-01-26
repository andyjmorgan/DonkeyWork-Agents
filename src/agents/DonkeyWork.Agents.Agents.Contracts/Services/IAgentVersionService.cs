using DonkeyWork.Agents.Agents.Contracts.Models;

namespace DonkeyWork.Agents.Agents.Contracts.Services;

/// <summary>
/// Service for managing agent versions.
/// </summary>
public interface IAgentVersionService
{
    /// <summary>
    /// Saves a draft version (creates new or updates existing draft).
    /// </summary>
    Task<GetAgentVersionResponseV1> SaveDraftAsync(Guid agentId, SaveAgentVersionRequestV1 request, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes the current draft version.
    /// </summary>
    Task<GetAgentVersionResponseV1> PublishAsync(Guid agentId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific version by ID.
    /// </summary>
    Task<GetAgentVersionResponseV1?> GetVersionAsync(Guid agentId, Guid versionId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all versions for an agent.
    /// </summary>
    Task<IReadOnlyList<GetAgentVersionResponseV1>> GetVersionsAsync(Guid agentId, Guid userId, CancellationToken cancellationToken = default);
}
