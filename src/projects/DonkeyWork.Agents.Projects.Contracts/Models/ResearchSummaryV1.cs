using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Projects.Contracts.Models;

/// <summary>
/// Research response model (summary for list views - excludes full content to reduce payload size).
/// Use research_get to retrieve full content.
/// </summary>
public sealed class ResearchSummaryV1
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("planPreview")]
    public string? PlanPreview { get; init; }

    [JsonPropertyName("planLength")]
    public int PlanLength { get; init; }

    [JsonPropertyName("status")]
    public ResearchStatus Status { get; init; }

    [JsonPropertyName("completedAt")]
    public DateTimeOffset? CompletedAt { get; init; }

    [JsonPropertyName("tags")]
    public List<TagV1> Tags { get; init; } = [];

    [JsonPropertyName("noteCount")]
    public int NoteCount { get; init; }

    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public DateTimeOffset? UpdatedAt { get; init; }
}
