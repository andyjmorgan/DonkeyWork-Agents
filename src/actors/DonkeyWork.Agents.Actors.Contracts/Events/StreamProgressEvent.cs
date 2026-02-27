namespace DonkeyWork.Agents.Actors.Contracts.Events;

[GenerateSerializer]
public sealed record StreamProgressEvent(
    string AgentKey,
    [property: Id(0)] string Breadcrumb) : StreamEventBase(AgentKey)
{
    public override string EventType => "progress";
}
