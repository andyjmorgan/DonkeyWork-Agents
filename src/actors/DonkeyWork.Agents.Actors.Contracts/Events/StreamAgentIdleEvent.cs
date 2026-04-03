namespace DonkeyWork.Agents.Actors.Contracts.Events;

[GenerateSerializer]
public sealed record StreamAgentIdleEvent(
    string AgentKey) : StreamEventBase(AgentKey)
{
    public override string EventType => "agent_idle";
}
