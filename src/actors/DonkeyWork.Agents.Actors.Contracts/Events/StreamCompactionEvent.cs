using MessagePack;
namespace DonkeyWork.Agents.Actors.Contracts.Events;

[GenerateSerializer]
[MessagePackObject(keyAsPropertyName: true)]
public sealed record StreamCompactionEvent(
    string AgentKey,
    [property: Id(0)] string? Summary) : StreamEventBase(AgentKey)
{
    public override string EventType => "compaction";
}
