using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Projects.Contracts.Models;

/// <summary>
/// File reference model.
/// </summary>
public sealed class FileReferenceV1
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("filePath")]
    public required string FilePath { get; init; }

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("sortOrder")]
    public int SortOrder { get; init; }
}

/// <summary>
/// Request to create or update a file reference.
/// </summary>
public sealed class FileReferenceRequestV1
{
    [JsonPropertyName("filePath")]
    [Required]
    [StringLength(1000, MinimumLength = 1)]
    public required string FilePath { get; init; }

    [JsonPropertyName("displayName")]
    [StringLength(255)]
    public string? DisplayName { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("sortOrder")]
    public int SortOrder { get; init; }
}
