using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Projects.Contracts.Models;

/// <summary>
/// Request to create a project.
/// </summary>
public sealed class CreateProjectRequestV1
{
    [JsonPropertyName("name")]
    [Required]
    [StringLength(255, MinimumLength = 1)]
    public required string Name { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("successCriteria")]
    public string? SuccessCriteria { get; init; }

    [JsonPropertyName("status")]
    public ProjectStatus Status { get; init; } = ProjectStatus.NotStarted;

    [JsonPropertyName("tags")]
    public List<TagRequestV1>? Tags { get; init; }

    [JsonPropertyName("fileReferences")]
    public List<FileReferenceRequestV1>? FileReferences { get; init; }
}

/// <summary>
/// Request to update a project.
/// </summary>
public sealed class UpdateProjectRequestV1
{
    [JsonPropertyName("name")]
    [Required]
    [StringLength(255, MinimumLength = 1)]
    public required string Name { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("successCriteria")]
    public string? SuccessCriteria { get; init; }

    [JsonPropertyName("status")]
    public ProjectStatus Status { get; init; }

    [JsonPropertyName("tags")]
    public List<TagRequestV1>? Tags { get; init; }

    [JsonPropertyName("fileReferences")]
    public List<FileReferenceRequestV1>? FileReferences { get; init; }
}

/// <summary>
/// Project response model (summary).
/// </summary>
public sealed class ProjectSummaryV1
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("status")]
    public ProjectStatus Status { get; init; }

    [JsonPropertyName("tags")]
    public List<TagV1> Tags { get; init; } = [];

    [JsonPropertyName("milestoneCount")]
    public int MilestoneCount { get; init; }

    [JsonPropertyName("todoCount")]
    public int TodoCount { get; init; }

    [JsonPropertyName("completedTodoCount")]
    public int CompletedTodoCount { get; init; }

    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public DateTimeOffset? UpdatedAt { get; init; }
}

/// <summary>
/// Project response model (full details).
/// </summary>
public sealed class ProjectDetailsV1
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("successCriteria")]
    public string? SuccessCriteria { get; init; }

    [JsonPropertyName("status")]
    public ProjectStatus Status { get; init; }

    [JsonPropertyName("tags")]
    public List<TagV1> Tags { get; init; } = [];

    [JsonPropertyName("fileReferences")]
    public List<FileReferenceV1> FileReferences { get; init; } = [];

    [JsonPropertyName("milestones")]
    public List<MilestoneSummaryV1> Milestones { get; init; } = [];

    [JsonPropertyName("todos")]
    public List<TodoV1> Todos { get; init; } = [];

    [JsonPropertyName("notes")]
    public List<NoteV1> Notes { get; init; } = [];

    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public DateTimeOffset? UpdatedAt { get; init; }
}
