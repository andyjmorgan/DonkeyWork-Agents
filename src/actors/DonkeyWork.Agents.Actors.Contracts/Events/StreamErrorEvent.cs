namespace DonkeyWork.Agents.Actors.Contracts.Events;

[GenerateSerializer]
public sealed record StreamErrorEvent(string AgentKey, [property: Id(0)] string Error) : StreamEventBase(AgentKey)
{
    public override string EventType => "error";
}
