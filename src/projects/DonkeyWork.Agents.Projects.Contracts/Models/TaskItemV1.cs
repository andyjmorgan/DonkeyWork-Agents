using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Projects.Contracts.Models;

/// <summary>
/// Request to create a task item.
/// </summary>
public sealed class CreateTaskItemRequestV1
{
    [JsonPropertyName("title")]
    [Required]
    [StringLength(500, MinimumLength = 1)]
    public required string Title { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("status")]
    public TaskItemStatus Status { get; init; } = TaskItemStatus.Pending;

    [JsonPropertyName("priority")]
    public TaskItemPriority Priority { get; init; } = TaskItemPriority.Medium;

    [JsonPropertyName("sortOrder")]
    public int SortOrder { get; init; }

    [JsonPropertyName("projectId")]
    public Guid? ProjectId { get; init; }

    [JsonPropertyName("milestoneId")]
    public Guid? MilestoneId { get; init; }

    [JsonPropertyName("tags")]
    public List<TagRequestV1>? Tags { get; init; }
}

/// <summary>
/// Request to update a task item.
/// </summary>
public sealed class UpdateTaskItemRequestV1
{
    [JsonPropertyName("title")]
    [Required]
    [StringLength(500, MinimumLength = 1)]
    public required string Title { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("status")]
    public TaskItemStatus Status { get; init; }

    [JsonPropertyName("priority")]
    public TaskItemPriority Priority { get; init; }

    [JsonPropertyName("completionNotes")]
    public string? CompletionNotes { get; init; }

    [JsonPropertyName("sortOrder")]
    public int SortOrder { get; init; }

    [JsonPropertyName("projectId")]
    public Guid? ProjectId { get; init; }

    [JsonPropertyName("milestoneId")]
    public Guid? MilestoneId { get; init; }

    [JsonPropertyName("tags")]
    public List<TagRequestV1>? Tags { get; init; }
}

/// <summary>
/// Task item response model.
/// </summary>
public sealed class TaskItemV1
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("descriptionLength")]
    public int DescriptionLength { get; init; }

    [JsonPropertyName("status")]
    public TaskItemStatus Status { get; init; }

    [JsonPropertyName("priority")]
    public TaskItemPriority Priority { get; init; }

    [JsonPropertyName("completionNotes")]
    public string? CompletionNotes { get; init; }

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
