using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Projects.Contracts.Models;

/// <summary>
/// Request to create a research item.
/// </summary>
public sealed class CreateResearchRequestV1
{
    [JsonPropertyName("title")]
    [Required]
    [StringLength(500, MinimumLength = 1)]
    public required string Title { get; init; }

    [JsonPropertyName("plan")]
    [Required]
    public required string Plan { get; init; }

    [JsonPropertyName("status")]
    public ResearchStatus Status { get; init; } = ResearchStatus.NotStarted;

    [JsonPropertyName("tags")]
    public List<TagRequestV1>? Tags { get; init; }
}

/// <summary>
/// Request to update a research item.
/// </summary>
public sealed class UpdateResearchRequestV1
{
    [JsonPropertyName("title")]
    [Required]
    [StringLength(500, MinimumLength = 1)]
    public required string Title { get; init; }

    [JsonPropertyName("plan")]
    public string? Plan { get; init; }

    [JsonPropertyName("result")]
    public string? Result { get; init; }

    [JsonPropertyName("status")]
    public ResearchStatus Status { get; init; }

    [JsonPropertyName("tags")]
    public List<TagRequestV1>? Tags { get; init; }
}
