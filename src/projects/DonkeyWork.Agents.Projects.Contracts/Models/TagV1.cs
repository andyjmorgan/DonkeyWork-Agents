using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Projects.Contracts.Models;

/// <summary>
/// Tag model.
/// </summary>
public sealed class TagV1
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("color")]
    public string? Color { get; init; }
}

/// <summary>
/// Request to create or update a tag.
/// </summary>
public sealed class TagRequestV1
{
    [JsonPropertyName("name")]
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public required string Name { get; init; }

    [JsonPropertyName("color")]
    [StringLength(7)]
    public string? Color { get; init; }
}
