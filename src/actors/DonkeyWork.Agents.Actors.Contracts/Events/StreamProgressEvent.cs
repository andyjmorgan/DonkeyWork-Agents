using MessagePack;
namespace DonkeyWork.Agents.Actors.Contracts.Events;

[GenerateSerializer]
[MessagePackObject(keyAsPropertyName: true)]
public sealed record StreamProgressEvent(
    string AgentKey,
    [property: Id(0)] string Breadcrumb) : StreamEventBase(AgentKey)
{
    public override string EventType => "progress";
}
