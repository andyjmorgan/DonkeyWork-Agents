using MessagePack;
namespace DonkeyWork.Agents.Actors.Contracts.Events;

[GenerateSerializer]
public enum AgentCompleteReason
{
    Completed,
    Cancelled,
    Failed,
}

[GenerateSerializer]
[MessagePackObject(keyAsPropertyName: true)]
public sealed record StreamAgentCompleteEvent(string AgentKey) : StreamEventBase(AgentKey)
{
    [Id(1)] public AgentCompleteReason Reason { get; init; }
    public override string EventType => "agent_complete";
}
