using MessagePack;
namespace DonkeyWork.Agents.Actors.Contracts.Events;

[GenerateSerializer]
[MessagePackObject(keyAsPropertyName: true)]
public sealed record StreamTurnStartEvent(
    string AgentKey,
    [property: Id(0)] string Source,
    [property: Id(1)] string MessagePreview) : StreamEventBase(AgentKey)
{
    public override string EventType => "turn_start";
}
