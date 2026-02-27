namespace DonkeyWork.Agents.Actors.Contracts.Events;

[GenerateSerializer]
public sealed record StreamCitationEvent(
    string AgentKey,
    [property: Id(0)] string Title,
    [property: Id(1)] string Url,
    [property: Id(2)] string CitedText) : StreamEventBase(AgentKey)
{
    public override string EventType => "citation";
}
