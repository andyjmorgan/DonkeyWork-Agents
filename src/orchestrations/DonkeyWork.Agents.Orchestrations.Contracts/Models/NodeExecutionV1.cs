using System.Text.Json.Serialization;
using DonkeyWork.Agents.Orchestrations.Contracts.Enums;
using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Enums;

namespace DonkeyWork.Agents.Orchestrations.Contracts.Models;

/// <summary>
/// Represents a single node execution within an orchestration execution.
/// </summary>
public class NodeExecutionV1
{
    /// <summary>
    /// Node execution ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Node ID from the orchestration configuration.
    /// </summary>
    public Guid NodeId { get; set; }

    /// <summary>
    /// Node type.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public NodeType NodeType { get; set; }

    /// <summary>
    /// Node name from configuration (user-friendly name).
    /// </summary>
    public string NodeName { get; set; } = string.Empty;

    /// <summary>
    /// For action nodes, the specific action type (e.g., "http_request", "sleep").
    /// </summary>
    public string? ActionType { get; set; }

    /// <summary>
    /// Execution status.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ExecutionStatus Status { get; set; }

    /// <summary>
    /// Input to this node (if available).
    /// </summary>
    public string? Input { get; set; }

    /// <summary>
    /// Output from this node.
    /// </summary>
    public string? Output { get; set; }

    /// <summary>
    /// Error message if node execution failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Timestamp when node execution started.
    /// </summary>
    public DateTimeOffset StartedAt { get; set; }

    /// <summary>
    /// Timestamp when node execution completed.
    /// </summary>
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>
    /// Execution duration in milliseconds.
    /// </summary>
    public int? DurationMs { get; set; }

    /// <summary>
    /// Tokens used (for model nodes).
    /// </summary>
    public int? TokensUsed { get; set; }

    /// <summary>
    /// Full LLM response (for model nodes).
    /// </summary>
    public string? FullResponse { get; set; }
}
