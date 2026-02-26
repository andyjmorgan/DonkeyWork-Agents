namespace DonkeyWork.Agents.Orleans.Contracts.Events;

[GenerateSerializer]
public sealed record StreamAgentSpawnEvent(
    string AgentKey,
    [property: Id(0)] string SpawnedAgentKey,
    [property: Id(1)] string AgentType) : StreamEventBase(AgentKey)
{
    [Id(2)] public string? Label { get; init; }
    public override string EventType => "agent_spawn";
}
