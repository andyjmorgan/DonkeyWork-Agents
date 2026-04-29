using MessagePack;
namespace DonkeyWork.Agents.Actors.Contracts.Events;

[GenerateSerializer]
[MessagePackObject(keyAsPropertyName: true)]
public sealed record StreamErrorEvent(string AgentKey, [property: Id(0)] string Error) : StreamEventBase(AgentKey)
{
    public override string EventType => "error";
}
