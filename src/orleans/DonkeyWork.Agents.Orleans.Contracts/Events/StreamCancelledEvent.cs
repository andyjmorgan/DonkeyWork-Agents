namespace DonkeyWork.Agents.Orleans.Contracts.Events;

[GenerateSerializer]
public sealed record StreamCancelledEvent(
    string AgentKey,
    [property: Id(0)] string Scope) : StreamEventBase(AgentKey)
{
    public override string EventType => "cancelled";
}
