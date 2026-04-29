using MessagePack;
namespace DonkeyWork.Agents.Actors.Contracts.Events;

[GenerateSerializer]
[MessagePackObject(keyAsPropertyName: true)]
public sealed record StreamAgentIdleEvent(
    string AgentKey) : StreamEventBase(AgentKey)
{
    public override string EventType => "agent_idle";
}
