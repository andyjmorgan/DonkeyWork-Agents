using System.Text.Json.Serialization;
using DonkeyWork.Agents.Orchestrations.Contracts.Enums;

namespace DonkeyWork.Agents.Orchestrations.Contracts.Models;

/// <summary>
/// Response from non-streaming agent execution.
/// </summary>
public sealed class ExecuteOrchestrationResponseV1
{
    /// <summary>
    /// The execution ID.
    /// </summary>
    [JsonPropertyName("executionId")]
    public required Guid ExecutionId { get; init; }

    /// <summary>
    /// The execution status (Completed, Failed).
    /// </summary>
    [JsonPropertyName("status")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required ExecutionStatus Status { get; init; }

    /// <summary>
    /// The output data if execution completed successfully.
    /// </summary>
    [JsonPropertyName("output")]
    public string? Output { get; init; }

    /// <summary>
    /// The error message if execution failed.
    /// </summary>
    [JsonPropertyName("error")]
    public string? Error { get; init; }
}
