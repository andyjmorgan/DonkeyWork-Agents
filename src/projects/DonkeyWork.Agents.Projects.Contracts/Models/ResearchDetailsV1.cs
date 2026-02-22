using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Projects.Contracts.Models;

/// <summary>
/// Research response model (full details).
/// </summary>
public sealed class ResearchDetailsV1
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("subject")]
    public required string Subject { get; init; }

    [JsonPropertyName("content")]
    public string? Content { get; init; }

    [JsonPropertyName("contentLength")]
    public int ContentLength { get; init; }

    [JsonPropertyName("summary")]
    public string? Summary { get; init; }

    [JsonPropertyName("summaryLength")]
    public int SummaryLength { get; init; }

    [JsonPropertyName("status")]
    public ResearchStatus Status { get; init; }

    [JsonPropertyName("completionNotes")]
    public string? CompletionNotes { get; init; }

    [JsonPropertyName("completedAt")]
    public DateTimeOffset? CompletedAt { get; init; }

    [JsonPropertyName("tags")]
    public List<TagV1> Tags { get; init; } = [];

    [JsonPropertyName("notes")]
    public List<NoteSummaryV1> Notes { get; init; } = [];

    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public DateTimeOffset? UpdatedAt { get; init; }
}
