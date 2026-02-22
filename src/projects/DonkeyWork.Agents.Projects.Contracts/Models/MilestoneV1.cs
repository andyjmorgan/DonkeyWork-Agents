using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Projects.Contracts.Models;

/// <summary>
/// Request to create a milestone.
/// </summary>
public sealed class CreateMilestoneRequestV1
{
    [JsonPropertyName("name")]
    [Required]
    [StringLength(255, MinimumLength = 1)]
    public required string Name { get; init; }

    [JsonPropertyName("content")]
    public string? Content { get; init; }

    [JsonPropertyName("successCriteria")]
    public string? SuccessCriteria { get; init; }

    [JsonPropertyName("status")]
    public MilestoneStatus Status { get; init; } = MilestoneStatus.NotStarted;

    [JsonPropertyName("dueDate")]
    public DateTimeOffset? DueDate { get; init; }

    [JsonPropertyName("sortOrder")]
    public int SortOrder { get; init; }

    [JsonPropertyName("tags")]
    public List<TagRequestV1>? Tags { get; init; }

    [JsonPropertyName("fileReferences")]
    public List<FileReferenceRequestV1>? FileReferences { get; init; }
}

/// <summary>
/// Request to update a milestone.
/// </summary>
public sealed class UpdateMilestoneRequestV1
{
    [JsonPropertyName("name")]
    [Required]
    [StringLength(255, MinimumLength = 1)]
    public required string Name { get; init; }

    [JsonPropertyName("content")]
    public string? Content { get; init; }

    [JsonPropertyName("successCriteria")]
    public string? SuccessCriteria { get; init; }

    [JsonPropertyName("status")]
    public MilestoneStatus Status { get; init; }

    [JsonPropertyName("completionNotes")]
    public string? CompletionNotes { get; init; }

    [JsonPropertyName("dueDate")]
    public DateTimeOffset? DueDate { get; init; }

    [JsonPropertyName("sortOrder")]
    public int SortOrder { get; init; }

    [JsonPropertyName("tags")]
    public List<TagRequestV1>? Tags { get; init; }

    [JsonPropertyName("fileReferences")]
    public List<FileReferenceRequestV1>? FileReferences { get; init; }
}

/// <summary>
/// Milestone response model (summary for list views - excludes content and successCriteria to reduce payload size).
/// Use milestones_get to retrieve full details.
/// </summary>
public sealed class MilestoneSummaryV1
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("projectId")]
    public Guid ProjectId { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("status")]
    public MilestoneStatus Status { get; init; }

    [JsonPropertyName("dueDate")]
    public DateTimeOffset? DueDate { get; init; }

    [JsonPropertyName("sortOrder")]
    public int SortOrder { get; init; }

    [JsonPropertyName("tags")]
    public List<TagV1> Tags { get; init; } = [];

    [JsonPropertyName("taskCount")]
    public int TaskItemCount { get; init; }

    [JsonPropertyName("completedTaskCount")]
    public int CompletedTaskItemCount { get; init; }

    [JsonPropertyName("contentPreview")]
    public string? ContentPreview { get; init; }

    [JsonPropertyName("contentLength")]
    public int ContentLength { get; init; }

    [JsonPropertyName("completedAt")]
    public DateTimeOffset? CompletedAt { get; init; }

    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public DateTimeOffset? UpdatedAt { get; init; }
}

/// <summary>
/// Milestone response model (full details).
/// </summary>
public sealed class MilestoneDetailsV1
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("projectId")]
    public Guid ProjectId { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("content")]
    public string? Content { get; init; }

    [JsonPropertyName("contentLength")]
    public int ContentLength { get; init; }

    [JsonPropertyName("successCriteria")]
    public string? SuccessCriteria { get; init; }

    [JsonPropertyName("status")]
    public MilestoneStatus Status { get; init; }

    [JsonPropertyName("dueDate")]
    public DateTimeOffset? DueDate { get; init; }

    [JsonPropertyName("sortOrder")]
    public int SortOrder { get; init; }

    [JsonPropertyName("tags")]
    public List<TagV1> Tags { get; init; } = [];

    [JsonPropertyName("fileReferences")]
    public List<FileReferenceV1> FileReferences { get; init; } = [];

    [JsonPropertyName("tasks")]
    public List<TaskItemV1> Tasks { get; init; } = [];

    [JsonPropertyName("notes")]
    public List<NoteV1> Notes { get; init; } = [];

    [JsonPropertyName("completionNotes")]
    public string? CompletionNotes { get; init; }

    [JsonPropertyName("completedAt")]
    public DateTimeOffset? CompletedAt { get; init; }

    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public DateTimeOffset? UpdatedAt { get; init; }
}
