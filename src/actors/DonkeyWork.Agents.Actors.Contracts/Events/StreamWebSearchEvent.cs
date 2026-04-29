using MessagePack;
namespace DonkeyWork.Agents.Actors.Contracts.Events;

[GenerateSerializer]
[MessagePackObject(keyAsPropertyName: true)]
public sealed record StreamWebSearchEvent(
    string AgentKey,
    [property: Id(0)] string ToolUseId,
    [property: Id(1)] string? Query = null) : StreamEventBase(AgentKey)
{
    public override string EventType => "web_search";
}
