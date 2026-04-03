namespace DonkeyWork.Agents.Actors.Contracts.Models;

[GenerateSerializer]
public record TrackedAgent(
    [property: Id(0)] string AgentKey,
    [property: Id(1)] string Label,
    [property: Id(2)] string ParentAgentKey,
    [property: Id(3)] AgentStatus Status,
    [property: Id(4)] AgentResult? Result,
    [property: Id(5)] DateTime SpawnedAt,
    [property: Id(6)] string Name = "");
