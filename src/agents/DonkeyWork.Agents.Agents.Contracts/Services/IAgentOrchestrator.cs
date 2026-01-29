namespace DonkeyWork.Agents.Agents.Contracts.Services;

/// <summary>
/// Service for orchestrating agent execution.
/// </summary>
public interface IAgentOrchestrator
{
    /// <summary>
    /// Executes an agent version with the provided input.
    /// </summary>
    /// <param name="executionId">The execution ID (caller-provided).</param>
    /// <param name="userId">The user ID executing the agent.</param>
    /// <param name="versionId">The agent version ID to execute.</param>
    /// <param name="input">The input data for the execution.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ExecuteAsync(
        Guid executionId,
        Guid userId,
        Guid versionId,
        object input,
        CancellationToken cancellationToken = default);
}
