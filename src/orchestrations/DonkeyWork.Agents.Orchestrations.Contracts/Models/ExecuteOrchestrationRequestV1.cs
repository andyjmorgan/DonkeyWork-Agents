using System.Text.Json;
using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Orchestrations.Contracts.Models;

/// <summary>
/// Request to execute an orchestration.
/// </summary>
public sealed class ExecuteOrchestrationRequestV1
{
    /// <summary>
    /// Dynamic JSON input data for the orchestration.
    /// </summary>
    [JsonPropertyName("input")]
    public required JsonElement Input { get; init; }

    /// <summary>
    /// Optional version override. If not specified, uses latest published (for execute) or draft (for test).
    /// </summary>
    [JsonPropertyName("versionId")]
    public Guid? VersionId { get; init; }
}
