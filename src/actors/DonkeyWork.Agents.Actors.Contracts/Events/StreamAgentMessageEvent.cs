namespace DonkeyWork.Agents.Actors.Contracts.Events;

[GenerateSerializer]
public sealed record StreamAgentMessageEvent(
    string AgentKey,
    [property: Id(0)] string FromAgentKey,
    [property: Id(1)] string FromName,
    [property: Id(2)] string ToAgentKey,
    [property: Id(3)] string ToName) : StreamEventBase(AgentKey)
{
    public override string EventType => "agent_message";
}
