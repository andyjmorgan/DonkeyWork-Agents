using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Orchestrations.Contracts.Models;

/// <summary>
/// Response containing a paginated list of executions.
/// </summary>
public sealed class ListExecutionsResponseV1
{
    /// <summary>
    /// The list of executions.
    /// </summary>
    [JsonPropertyName("executions")]
    public required IReadOnlyList<GetExecutionResponseV1> Executions { get; init; }

    /// <summary>
    /// The total count of executions.
    /// </summary>
    [JsonPropertyName("totalCount")]
    public required int TotalCount { get; init; }
}
