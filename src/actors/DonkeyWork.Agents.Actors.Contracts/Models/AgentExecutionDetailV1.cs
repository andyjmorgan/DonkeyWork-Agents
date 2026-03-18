namespace DonkeyWork.Agents.Actors.Contracts.Models;

/// <summary>
/// Full detail view of an agent execution, including contract snapshot and output.
/// </summary>
public sealed class AgentExecutionDetailV1
{
    public Guid Id { get; init; }
    public Guid ConversationId { get; init; }
    public required string AgentType { get; init; }
    public required string Label { get; init; }
    public required string GrainKey { get; init; }
    public string? ParentGrainKey { get; init; }
    public required string ContractSnapshot { get; init; }
    public string? Input { get; init; }
    public string? Output { get; init; }
    public required string Status { get; init; }
    public string? ErrorMessage { get; init; }
    public string? ModelId { get; init; }
    public DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public long? DurationMs { get; init; }
    public int? InputTokensUsed { get; init; }
    public int? OutputTokensUsed { get; init; }
}
