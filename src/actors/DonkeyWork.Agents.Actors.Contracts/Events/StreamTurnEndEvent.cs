namespace DonkeyWork.Agents.Actors.Contracts.Events;

[GenerateSerializer]
public sealed record StreamTurnEndEvent(string AgentKey) : StreamEventBase(AgentKey)
{
    public override string EventType => "turn_end";
}
