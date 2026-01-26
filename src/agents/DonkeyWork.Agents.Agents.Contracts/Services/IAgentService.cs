using DonkeyWork.Agents.Agents.Contracts.Models;

namespace DonkeyWork.Agents.Agents.Contracts.Services;

/// <summary>
/// Service for managing agents.
/// </summary>
public interface IAgentService
{
    /// <summary>
    /// Creates a new agent with an initial draft version (Start -> End template).
    /// </summary>
    Task<CreateAgentResponseV1> CreateAsync(CreateAgentRequestV1 request, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an agent by ID.
    /// </summary>
    Task<GetAgentResponseV1?> GetByIdAsync(Guid agentId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all agents for a user.
    /// </summary>
    Task<IReadOnlyList<GetAgentResponseV1>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates agent metadata (name, description).
    /// </summary>
    Task<GetAgentResponseV1?> UpdateAsync(Guid agentId, CreateAgentRequestV1 request, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an agent and all its versions.
    /// </summary>
    Task<bool> DeleteAsync(Guid agentId, Guid userId, CancellationToken cancellationToken = default);
}
