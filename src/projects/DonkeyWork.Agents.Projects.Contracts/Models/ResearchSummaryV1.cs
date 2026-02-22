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

    [JsonPropertyName("subject")]
    public required string Subject { get; init; }

    [JsonPropertyName("contentPreview")]
    public string? ContentPreview { get; init; }

    [JsonPropertyName("contentLength")]
    public int ContentLength { get; init; }

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
