using MessagePack;
namespace DonkeyWork.Agents.Actors.Contracts.Events;

[GenerateSerializer]
[MessagePackObject(keyAsPropertyName: true)]
public sealed record StreamMessageEvent(string AgentKey, [property: Id(0)] string Text) : StreamEventBase(AgentKey)
{
    public override string EventType => "message";
}
