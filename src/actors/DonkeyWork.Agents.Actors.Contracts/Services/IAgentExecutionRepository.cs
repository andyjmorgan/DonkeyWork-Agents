using DonkeyWork.Agents.Actors.Contracts.Models;

namespace DonkeyWork.Agents.Actors.Contracts.Services;

/// <summary>
/// Low-level persistence for agent execution records.
/// Takes explicit userId parameters (grain context has no scoped IIdentityContext).
/// </summary>
public interface IAgentExecutionRepository
{
    /// <summary>
    /// Creates a new execution record with status "Running".
    /// </summary>
    Task<Guid> CreateAsync(
        Guid userId,
        Guid conversationId,
        string agentType,
        string label,
        string grainKey,
        string? parentGrainKey,
        string contractSnapshot,
        string? input,
        string? modelId,
        Guid turnId = default,
        CancellationToken ct = default);

    /// <summary>
    /// Updates an execution with completion data.
    /// </summary>
    Task UpdateCompletionAsync(
        Guid executionId,
        Guid userId,
        string status,
        string? output,
        string? errorMessage,
        long? durationMs,
        int? inputTokens,
        int? outputTokens,
        CancellationToken ct = default);

    /// <summary>
    /// Updates the execution status and timestamps without setting completion data.
    /// </summary>
    Task UpdateStatusAsync(
        Guid executionId,
        Guid userId,
        string status,
        CancellationToken ct = default);

    /// <summary>
    /// Updates token counters incrementally during execution.
    /// </summary>
    Task UpdateTokensAsync(
        Guid executionId,
        Guid userId,
        int inputTokens,
        int outputTokens,
        CancellationToken ct = default);

    /// <summary>
    /// Gets execution detail by ID.
    /// </summary>
    Task<AgentExecutionDetailV1?> GetByIdAsync(Guid executionId, Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Gets execution detail by grain key.
    /// </summary>
    Task<AgentExecutionDetailV1?> GetByGrainKeyAsync(string grainKey, Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Lists all executions for a conversation, ordered by started_at.
    /// </summary>
    Task<IReadOnlyList<AgentExecutionSummaryV1>> ListByConversationAsync(
        Guid conversationId, Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Lists all executions for a user with pagination, ordered by started_at descending.
    /// </summary>
    Task<(IReadOnlyList<AgentExecutionSummaryV1> Items, int TotalCount)> ListAsync(
        Guid userId, int offset, int limit, CancellationToken ct = default);
}
