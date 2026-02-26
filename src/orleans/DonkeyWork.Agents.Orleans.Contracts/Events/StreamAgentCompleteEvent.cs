namespace DonkeyWork.Agents.Orleans.Contracts.Events;

[GenerateSerializer]
public enum AgentCompleteReason
{
    Completed,
    Cancelled,
    Failed,
}

[GenerateSerializer]
public sealed record StreamAgentCompleteEvent(string AgentKey) : StreamEventBase(AgentKey)
{
    [Id(1)] public AgentCompleteReason Reason { get; init; }
    public override string EventType => "agent_complete";
}
