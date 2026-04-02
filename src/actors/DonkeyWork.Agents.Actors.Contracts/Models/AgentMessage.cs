namespace DonkeyWork.Agents.Actors.Contracts.Models;

[GenerateSerializer]
public sealed record AgentMessage(
    [property: Id(0)] string FromAgentKey,
    [property: Id(1)] string FromName,
    [property: Id(2)] string Content,
    [property: Id(3)] DateTimeOffset SentAt);
