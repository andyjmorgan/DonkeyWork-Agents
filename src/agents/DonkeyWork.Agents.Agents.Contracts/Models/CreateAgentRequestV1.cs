using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Agents.Contracts.Models;

/// <summary>
/// Request to create a new agent.
/// </summary>
public sealed class CreateAgentRequestV1
{
    /// <summary>
    /// Agent name. Must match pattern: lowercase a-z, 0-9, -, _ only.
    /// </summary>
    [JsonPropertyName("name")]
    [Required]
    [StringLength(255, MinimumLength = 1)]
    [RegularExpression("^[a-z0-9_-]+$", ErrorMessage = "Name must contain only lowercase letters, numbers, hyphens, and underscores")]
    public required string Name { get; init; }

    /// <summary>
    /// Agent description.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; init; }
}
