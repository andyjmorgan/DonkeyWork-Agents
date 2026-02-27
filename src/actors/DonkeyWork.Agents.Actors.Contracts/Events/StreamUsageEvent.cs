namespace DonkeyWork.Agents.Actors.Contracts.Events;

[GenerateSerializer]
public sealed record StreamUsageEvent(
    string AgentKey,
    [property: Id(0)] int InputTokens,
    [property: Id(1)] int OutputTokens,
    [property: Id(2)] int WebSearchRequests) : StreamEventBase(AgentKey)
{
    public override string EventType => "usage";
}
