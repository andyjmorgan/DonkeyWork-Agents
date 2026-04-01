using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Orchestrations.Contracts.Models;

/// <summary>
/// Request to create a new agent.
/// </summary>
public sealed class CreateOrchestrationRequestV1
{
    /// <summary>
    /// Orchestration name. Must match pattern: lowercase a-z, 0-9, -, _ only.
    /// </summary>
    [JsonPropertyName("name")]
    [Required]
    [StringLength(255, MinimumLength = 1)]
    [RegularExpression("^[a-z0-9_-]+$", ErrorMessage = "Name must contain only lowercase letters, numbers, hyphens, and underscores")]
    public required string Name { get; init; }

    /// <summary>
    /// Orchestration description.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("friendlyName")]
    [StringLength(255)]
    public string? FriendlyName { get; init; }
}
