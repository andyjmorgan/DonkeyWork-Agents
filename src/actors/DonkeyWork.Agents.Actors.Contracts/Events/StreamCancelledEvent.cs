using MessagePack;
namespace DonkeyWork.Agents.Actors.Contracts.Events;

[GenerateSerializer]
[MessagePackObject(keyAsPropertyName: true)]
public sealed record StreamCancelledEvent(
    string AgentKey,
    [property: Id(0)] string Scope) : StreamEventBase(AgentKey)
{
    public override string EventType => "cancelled";
}
