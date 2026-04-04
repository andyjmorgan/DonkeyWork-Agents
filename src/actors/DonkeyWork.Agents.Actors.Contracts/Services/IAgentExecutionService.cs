using DonkeyWork.Agents.Actors.Contracts.Models;
using DonkeyWork.Agents.Common.Contracts.Models.Pagination;

namespace DonkeyWork.Agents.Actors.Contracts.Services;

/// <summary>
/// API-facing service for agent execution queries. Uses IIdentityContext internally.
/// </summary>
public interface IAgentExecutionService
{
    /// <summary>
    /// Gets execution detail by ID.
    /// </summary>
    Task<AgentExecutionDetailV1?> GetByIdAsync(Guid executionId, CancellationToken ct = default);

    /// <summary>
    /// Lists all executions for a conversation, ordered by started_at.
    /// </summary>
    Task<IReadOnlyList<AgentExecutionSummaryV1>> ListByConversationAsync(
        Guid conversationId, CancellationToken ct = default);

    /// <summary>
    /// Lists all executions for the current user with pagination.
    /// </summary>
    Task<PaginatedResponse<AgentExecutionSummaryV1>> ListAsync(
        int offset, int limit, CancellationToken ct = default);

    /// <summary>
    /// Gets the message history for an execution.
    /// </summary>
    Task<GetAgentExecutionMessagesResponseV1?> GetMessagesAsync(Guid executionId, CancellationToken ct = default);
}
