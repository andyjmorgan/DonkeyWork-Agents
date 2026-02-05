using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Projects.Contracts.Models;

/// <summary>
/// Note response model (summary for list views - excludes content to reduce payload size).
/// Use notes_get to retrieve full content.
/// </summary>
public sealed class NoteSummaryV1
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("sortOrder")]
    public int SortOrder { get; init; }

    [JsonPropertyName("projectId")]
    public Guid? ProjectId { get; init; }

    [JsonPropertyName("milestoneId")]
    public Guid? MilestoneId { get; init; }

    [JsonPropertyName("tags")]
    public List<TagV1> Tags { get; init; } = [];

    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public DateTimeOffset? UpdatedAt { get; init; }
}
