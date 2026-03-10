namespace DonkeyWork.Agents.Actors.Contracts.Contracts;

[GenerateSerializer]
public sealed class SubAgentReference
{
    [Id(0)] public required string Id { get; init; }
    [Id(1)] public required string Name { get; init; }
    [Id(2)] public string? Description { get; init; }
}
