using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Orchestrations.Contracts.Models;

/// <summary>
/// Response after creating a new agent.
/// </summary>
public sealed class CreateOrchestrationResponseV1
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
    /// ID of the initial draft version created with the orchestration.
    /// </summary>
    [JsonPropertyName("versionId")]
    public required Guid VersionId { get; init; }

    /// <summary>
    /// Creation timestamp.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public required DateTimeOffset CreatedAt { get; init; }
}
