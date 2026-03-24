namespace DonkeyWork.Agents.Actors.Contracts.Events;

[GenerateSerializer]
public sealed record StreamCompactionEvent(
    string AgentKey,
    [property: Id(0)] string? Summary) : StreamEventBase(AgentKey)
{
    public override string EventType => "compaction";
}
