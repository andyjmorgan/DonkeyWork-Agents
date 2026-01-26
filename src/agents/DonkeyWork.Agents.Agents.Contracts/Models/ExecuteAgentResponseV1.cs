using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Agents.Contracts.Models;

/// <summary>
/// Response from non-streaming agent execution.
/// </summary>
public sealed class ExecuteAgentResponseV1
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
    public required string Status { get; init; }

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
