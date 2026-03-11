using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Projects.Contracts.Models;

/// <summary>
/// Research response model (full details).
/// </summary>
public sealed class ResearchDetailsV1
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("plan")]
    public string? Plan { get; init; }

    [JsonPropertyName("planLength")]
    public int PlanLength { get; init; }

    [JsonPropertyName("result")]
    public string? Result { get; init; }

    [JsonPropertyName("resultLength")]
    public int ResultLength { get; init; }

    [JsonPropertyName("status")]
    public ResearchStatus Status { get; init; }

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
