using DonkeyWork.Agents.Orchestrations.Contracts.Models;

namespace DonkeyWork.Agents.Orchestrations.Contracts.Services;

/// <summary>
/// Service for managing agents.
/// </summary>
public interface IOrchestrationService
{
    /// <summary>
    /// Creates a new agent with an initial draft version (Start -> End template).
    /// </summary>
    Task<CreateOrchestrationResponseV1> CreateAsync(CreateOrchestrationRequestV1 request, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an orchestration by ID.
    /// </summary>
    Task<GetOrchestrationResponseV1?> GetByIdAsync(Guid orchestrationId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all agents for a user.
    /// </summary>
    Task<IReadOnlyList<GetOrchestrationResponseV1>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates agent metadata (name, description).
    /// </summary>
    Task<GetOrchestrationResponseV1?> UpdateAsync(Guid orchestrationId, CreateOrchestrationRequestV1 request, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an orchestration and all its versions.
    /// </summary>
    Task<bool> DeleteAsync(Guid orchestrationId, Guid userId, CancellationToken cancellationToken = default);
}
