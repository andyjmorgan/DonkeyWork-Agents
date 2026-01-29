using DonkeyWork.Agents.Agents.Contracts.Models;

namespace DonkeyWork.Agents.Agents.Contracts.Services;

/// <summary>
/// Repository for agent execution data.
/// </summary>
public interface IAgentExecutionRepository
{
    /// <summary>
    /// Creates a new execution record.
    /// </summary>
    Task<GetExecutionResponseV1> CreateAsync(
        Guid agentId,
        Guid versionId,
        string input,
        string streamName,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an execution with completion data.
    /// </summary>
    Task UpdateCompletionAsync(
        Guid executionId,
        string status,
        string? output,
        string? errorMessage,
        int? totalTokensUsed,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an execution by ID.
    /// </summary>
    Task<GetExecutionResponseV1?> GetByIdAsync(
        Guid executionId,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists executions with optional filtering by agent.
    /// </summary>
    Task<(IReadOnlyList<GetExecutionResponseV1> Executions, int TotalCount)> ListAsync(
        Guid? agentId,
        int offset,
        int limit,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets execution logs for a specific execution.
    /// </summary>
    Task<(IReadOnlyList<ExecutionLogV1> Logs, int TotalCount)> GetLogsAsync(
        Guid executionId,
        int offset,
        int limit,
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets node executions for a specific execution.
    /// </summary>
    Task<(IReadOnlyList<NodeExecutionV1> NodeExecutions, int TotalCount)> GetNodeExecutionsAsync(
        Guid executionId,
        int offset,
        int limit,
        Guid userId,
        CancellationToken cancellationToken = default);
}
