namespace DonkeyWork.Agents.Orleans.Contracts.Events;

[GenerateSerializer]
public sealed record StreamToolCompleteEvent(
    string AgentKey,
    [property: Id(0)] string ToolUseId,
    [property: Id(1)] string ToolName,
    [property: Id(2)] bool Success,
    [property: Id(3)] long DurationMs) : StreamEventBase(AgentKey)
{
    [Id(4)] public string? DisplayName { get; init; }
    public override string EventType => "tool_complete";
}
