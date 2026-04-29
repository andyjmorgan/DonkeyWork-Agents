using MessagePack;
namespace DonkeyWork.Agents.Actors.Contracts.Events;

[GenerateSerializer]
[MessagePackObject(keyAsPropertyName: true)]
public sealed record StreamTurnEndEvent(string AgentKey) : StreamEventBase(AgentKey)
{
    public override string EventType => "turn_end";
}
