using MessagePack;
namespace DonkeyWork.Agents.Actors.Contracts.Events;

[GenerateSerializer]
[MessagePackObject(keyAsPropertyName: true)]
public sealed record StreamWebFetchEvent(
    string AgentKey,
    [property: Id(0)] string ToolUseId,
    [property: Id(1)] string? Url = null) : StreamEventBase(AgentKey)
{
    public override string EventType => "web_fetch";
}
