using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Projects.Contracts.Models;

/// <summary>
/// Request to create a research item.
/// </summary>
public sealed class CreateResearchRequestV1
{
    [JsonPropertyName("subject")]
    [Required]
    [StringLength(500, MinimumLength = 1)]
    public required string Subject { get; init; }

    [JsonPropertyName("content")]
    public string? Content { get; init; }

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
    [JsonPropertyName("subject")]
    [Required]
    [StringLength(500, MinimumLength = 1)]
    public required string Subject { get; init; }

    [JsonPropertyName("content")]
    public string? Content { get; init; }

    [JsonPropertyName("summary")]
    public string? Summary { get; init; }

    [JsonPropertyName("status")]
    public ResearchStatus Status { get; init; }

    [JsonPropertyName("completionNotes")]
    public string? CompletionNotes { get; init; }

    [JsonPropertyName("tags")]
    public List<TagRequestV1>? Tags { get; init; }
}
