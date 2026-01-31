using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Projects.Contracts.Models;

/// <summary>
/// Request to create a note.
/// </summary>
public sealed class CreateNoteRequestV1
{
    [JsonPropertyName("title")]
    [Required]
    [StringLength(500, MinimumLength = 1)]
    public required string Title { get; init; }

    [JsonPropertyName("content")]
    public string? Content { get; init; }

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
/// Request to update a note.
/// </summary>
public sealed class UpdateNoteRequestV1
{
    [JsonPropertyName("title")]
    [Required]
    [StringLength(500, MinimumLength = 1)]
    public required string Title { get; init; }

    [JsonPropertyName("content")]
    public string? Content { get; init; }

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
/// Note response model.
/// </summary>
public sealed class NoteV1
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("content")]
    public string? Content { get; init; }

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
