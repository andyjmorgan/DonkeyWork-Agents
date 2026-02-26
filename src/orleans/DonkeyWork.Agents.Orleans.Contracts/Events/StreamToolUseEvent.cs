namespace DonkeyWork.Agents.Orleans.Contracts.Events;

[GenerateSerializer]
public sealed record StreamToolUseEvent(
    string AgentKey,
    [property: Id(0)] string ToolName,
    [property: Id(1)] string ToolUseId,
    [property: Id(2)] string Arguments) : StreamEventBase(AgentKey)
{
    [Id(3)] public string? DisplayName { get; init; }
    public override string EventType => "tool_use";
}
