namespace DonkeyWork.Agents.Orleans.Contracts.Events;

[GenerateSerializer]
public abstract record StreamEventBase([property: Id(0)] string AgentKey)
{
    public abstract string EventType { get; }
}
