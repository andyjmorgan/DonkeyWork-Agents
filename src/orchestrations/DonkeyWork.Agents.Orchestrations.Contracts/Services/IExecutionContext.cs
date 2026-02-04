using System.Text.Json;
using DonkeyWork.Agents.Orchestrations.Contracts.Enums;
using DonkeyWork.Agents.Orchestrations.Contracts.Models;

namespace DonkeyWork.Agents.Orchestrations.Contracts.Services;

/// <summary>
/// Scoped context that maintains state during orchestration execution.
/// Hydrated by the executor before node execution begins.
/// </summary>
public interface IExecutionContext
{
    /// <summary>
    /// Unique identifier for this execution.
    /// </summary>
    Guid ExecutionId { get; }

    /// <summary>
    /// The user ID that owns this execution.
    /// </summary>
    Guid UserId { get; }

    /// <summary>
    /// The interface through which this execution was triggered.
    /// </summary>
    ExecutionInterface Interface { get; }

    /// <summary>
    /// The input provided to the execution (for Direct mode).
    /// Validated against the orchestration version's InputSchema.
    /// </summary>
    JsonElement Input { get; }

    /// <summary>
    /// The JSON Schema for input validation.
    /// </summary>
    JsonDocument InputSchema { get; }

    /// <summary>
    /// The conversation context (for Chat mode).
    /// Contains history, ID, and current user message.
    /// Null when not in Chat mode.
    /// </summary>
    ConversationContext? Conversation { get; }

    /// <summary>
    /// Dictionary of node outputs keyed by node name.
    /// Used for template variable resolution (accessible as 'steps' in Scriban).
    /// </summary>
    IReadOnlyDictionary<string, object> NodeOutputs { get; }

    /// <summary>
    /// Hydrates the context with execution details for Direct mode.
    /// </summary>
    void Hydrate(Guid executionId, Guid userId, ExecutionInterface executionInterface, JsonElement input, JsonDocument inputSchema);

    /// <summary>
    /// Hydrates the context with execution details for Chat mode.
    /// </summary>
    void HydrateChat(Guid executionId, Guid userId, ConversationContext conversation, JsonDocument inputSchema);

    /// <summary>
    /// Sets the output for a completed node.
    /// </summary>
    void SetNodeOutput(string nodeName, object output);
}
