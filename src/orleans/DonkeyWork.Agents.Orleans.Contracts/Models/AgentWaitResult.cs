namespace DonkeyWork.Agents.Orleans.Contracts.Models;

[GenerateSerializer]
public record AgentWaitResult(
    [property: Id(0)] string AgentKey,
    [property: Id(1)] string Label,
    [property: Id(2)] AgentResult Result,
    [property: Id(3)] AgentStatus Status);
