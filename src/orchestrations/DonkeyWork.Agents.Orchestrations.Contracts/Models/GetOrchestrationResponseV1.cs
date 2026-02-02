using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Orchestrations.Contracts.Models;

/// <summary>
/// Response containing full agent data with current version.
/// </summary>
public sealed class GetOrchestrationResponseV1
{
    /// <summary>
    /// Orchestration ID.
    /// </summary>
    [JsonPropertyName("id")]
    public required Guid Id { get; init; }

    /// <summary>
    /// Orchestration name.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// Orchestration description.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; init; }

    /// <summary>
    /// Current published version ID (null if no version published yet).
    /// </summary>
    [JsonPropertyName("currentVersionId")]
    public Guid? CurrentVersionId { get; init; }

    /// <summary>
    /// Creation timestamp.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Last update timestamp.
    /// </summary>
    [JsonPropertyName("updatedAt")]
    public DateTimeOffset? UpdatedAt { get; init; }
}
