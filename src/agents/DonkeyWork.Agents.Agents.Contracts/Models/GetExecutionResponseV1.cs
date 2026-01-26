using System.Text.Json;
using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Agents.Contracts.Models;

/// <summary>
/// Response containing execution details.
/// </summary>
public sealed class GetExecutionResponseV1
{
    /// <summary>
    /// The execution ID.
    /// </summary>
    [JsonPropertyName("id")]
    public required Guid Id { get; init; }

    /// <summary>
    /// The agent ID.
    /// </summary>
    [JsonPropertyName("agentId")]
    public required Guid AgentId { get; init; }

    /// <summary>
    /// The agent version ID that was executed.
    /// </summary>
    [JsonPropertyName("versionId")]
    public required Guid VersionId { get; init; }

    /// <summary>
    /// The execution status.
    /// </summary>
    [JsonPropertyName("status")]
    public required string Status { get; init; }

    /// <summary>
    /// The input data.
    /// </summary>
    [JsonPropertyName("input")]
    public required JsonElement Input { get; init; }

    /// <summary>
    /// The output data if execution completed successfully.
    /// </summary>
    [JsonPropertyName("output")]
    public JsonElement? Output { get; init; }

    /// <summary>
    /// The error message if execution failed.
    /// </summary>
    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// When the execution started.
    /// </summary>
    [JsonPropertyName("startedAt")]
    public required DateTimeOffset StartedAt { get; init; }

    /// <summary>
    /// When the execution completed (null if still running).
    /// </summary>
    [JsonPropertyName("completedAt")]
    public DateTimeOffset? CompletedAt { get; init; }

    /// <summary>
    /// Total tokens used across all model nodes.
    /// </summary>
    [JsonPropertyName("totalTokensUsed")]
    public int? TotalTokensUsed { get; init; }
}
