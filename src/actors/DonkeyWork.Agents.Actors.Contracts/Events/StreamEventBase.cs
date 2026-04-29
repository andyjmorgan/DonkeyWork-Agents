namespace DonkeyWork.Agents.Actors.Contracts.Events;

[GenerateSerializer]
public abstract record StreamEventBase([property: Id(0)] string AgentKey)
{
    public abstract string EventType { get; }
    [Id(1)] public Guid TurnId { get; init; }
}
