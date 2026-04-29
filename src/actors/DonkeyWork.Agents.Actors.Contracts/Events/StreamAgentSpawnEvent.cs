using MessagePack;
namespace DonkeyWork.Agents.Actors.Contracts.Events;

[GenerateSerializer]
[MessagePackObject(keyAsPropertyName: true)]
public sealed record StreamAgentSpawnEvent(
    string AgentKey,
    [property: Id(0)] string SpawnedAgentKey,
    [property: Id(1)] string AgentType) : StreamEventBase(AgentKey)
{
    [Id(2)] public string? Label { get; init; }
    [Id(3)] public string? Icon { get; init; }
    [Id(4)] public string? DisplayName { get; init; }
    public override string EventType => "agent_spawn";
}
