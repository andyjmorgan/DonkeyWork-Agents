namespace DonkeyWork.Agents.Orleans.Contracts.Events;

[GenerateSerializer]
public sealed record StreamMessageEvent(string AgentKey, [property: Id(0)] string Text) : StreamEventBase(AgentKey)
{
    public override string EventType => "message";
}
