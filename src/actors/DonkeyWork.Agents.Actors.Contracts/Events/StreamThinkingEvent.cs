using MessagePack;
namespace DonkeyWork.Agents.Actors.Contracts.Events;

[GenerateSerializer]
[MessagePackObject(keyAsPropertyName: true)]
public sealed record StreamThinkingEvent(string AgentKey, [property: Id(0)] string Text) : StreamEventBase(AgentKey)
{
    public override string EventType => "thinking";
}
