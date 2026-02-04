using System.Text.Json;
using DonkeyWork.Agents.Orchestrations.Contracts.Enums;
using DonkeyWork.Agents.Orchestrations.Contracts.Models;

namespace DonkeyWork.Agents.Orchestrations.Contracts.Services;

/// <summary>
/// Service for executing orchestrations.
/// </summary>
public interface IOrchestrationExecutor
{
    /// <summary>
    /// Executes an orchestration version with the provided input (Direct mode).
    /// </summary>
    /// <param name="executionId">The execution ID (caller-provided).</param>
    /// <param name="userId">The user ID executing the orchestration.</param>
    /// <param name="versionId">The orchestration version ID to execute.</param>
    /// <param name="executionInterface">The interface through which this execution was triggered.</param>
    /// <param name="input">The input data for the execution.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ExecuteAsync(
        Guid executionId,
        Guid userId,
        Guid versionId,
        ExecutionInterface executionInterface,
        JsonElement input,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes an orchestration version with conversation context (Chat mode).
    /// </summary>
    /// <param name="executionId">The execution ID (caller-provided).</param>
    /// <param name="userId">The user ID executing the orchestration.</param>
    /// <param name="versionId">The orchestration version ID to execute.</param>
    /// <param name="conversation">The conversation context with history and current message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ExecuteChatAsync(
        Guid executionId,
        Guid userId,
        Guid versionId,
        ConversationContext conversation,
        CancellationToken cancellationToken = default);
}
