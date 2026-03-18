namespace DonkeyWork.Agents.Actors.Contracts.Models;

/// <summary>
/// Lightweight summary of an agent execution for list views.
/// </summary>
public sealed class AgentExecutionSummaryV1
{
    public Guid Id { get; init; }
    public Guid ConversationId { get; init; }
    public required string AgentType { get; init; }
    public required string Label { get; init; }
    public required string GrainKey { get; init; }
    public required string Status { get; init; }
    public string? ModelId { get; init; }
    public DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public long? DurationMs { get; init; }
    public int? InputTokensUsed { get; init; }
    public int? OutputTokensUsed { get; init; }
}
