using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Projects.Contracts.Models;

/// <summary>
/// Task item response model (summary for list views - excludes description and completionNotes to reduce payload size).
/// Use tasks_get to retrieve full details.
/// </summary>
public sealed class TaskItemSummaryV1
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("status")]
    public TaskItemStatus Status { get; init; }

    [JsonPropertyName("priority")]
    public TaskItemPriority Priority { get; init; }

    [JsonPropertyName("dueDate")]
    public DateTimeOffset? DueDate { get; init; }

    [JsonPropertyName("completedAt")]
    public DateTimeOffset? CompletedAt { get; init; }

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
