using MessagePack;
namespace DonkeyWork.Agents.Actors.Contracts.Events;

[GenerateSerializer]
[MessagePackObject(keyAsPropertyName: true)]
public sealed record StreamToolResultEvent(
    string AgentKey,
    [property: Id(0)] string ToolUseId,
    [property: Id(1)] string ToolName,
    [property: Id(2)] string Result,
    [property: Id(3)] bool Success,
    [property: Id(4)] long DurationMs) : StreamEventBase(AgentKey)
{
    [Id(5)] public string? DisplayName { get; init; }
    public override string EventType => "tool_result";
}
