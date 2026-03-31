namespace DonkeyWork.Agents.Actors.Contracts.Events;

[GenerateSerializer]
public sealed record StreamRetryEvent(
    string AgentKey,
    [property: Id(0)] int Attempt,
    [property: Id(1)] int MaxRetries,
    [property: Id(2)] int DelayMs,
    [property: Id(3)] string Reason) : StreamEventBase(AgentKey)
{
    public override string EventType => "retry";
}
