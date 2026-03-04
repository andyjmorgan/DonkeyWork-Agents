namespace DonkeyWork.Agents.Actors.Contracts.Events;

[GenerateSerializer]
public sealed record StreamSandboxStatusEvent(
    string AgentKey,
    [property: Id(0)] string Status,
    [property: Id(1)] string? Message,
    [property: Id(2)] string? PodName) : StreamEventBase(AgentKey)
{
    public override string EventType => "sandbox_status";
}
