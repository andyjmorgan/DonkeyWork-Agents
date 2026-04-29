using MessagePack;
namespace DonkeyWork.Agents.Actors.Contracts.Events;

[GenerateSerializer]
[MessagePackObject(keyAsPropertyName: true)]
public sealed record StreamUsageEvent(
    string AgentKey,
    [property: Id(0)] int InputTokens,
    [property: Id(1)] int OutputTokens,
    [property: Id(2)] int WebSearchRequests,
    [property: Id(3)] int ContextWindowLimit,
    [property: Id(4)] int MaxOutputTokens) : StreamEventBase(AgentKey)
{
    public override string EventType => "usage";
}
