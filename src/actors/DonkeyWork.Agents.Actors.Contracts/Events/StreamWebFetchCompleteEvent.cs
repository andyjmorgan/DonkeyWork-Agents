using MessagePack;
namespace DonkeyWork.Agents.Actors.Contracts.Events;

[GenerateSerializer]
[MessagePackObject(keyAsPropertyName: true)]
public sealed record StreamWebFetchCompleteEvent(
    string AgentKey,
    [property: Id(0)] string ToolUseId,
    [property: Id(1)] string Url,
    [property: Id(2)] string? Title) : StreamEventBase(AgentKey)
{
    public override string EventType => "web_fetch_complete";
}
