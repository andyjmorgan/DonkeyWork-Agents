namespace DonkeyWork.Agents.Actors.Contracts.Events;

[GenerateSerializer]
public sealed record StreamQueueStatusEvent(
    string AgentKey,
    [property: Id(0)] int PendingCount,
    [property: Id(1)] bool IsProcessing) : StreamEventBase(AgentKey)
{
    public override string EventType => "queue_status";
}
