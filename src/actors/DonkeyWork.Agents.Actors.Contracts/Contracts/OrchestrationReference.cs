namespace DonkeyWork.Agents.Actors.Contracts.Contracts;

[GenerateSerializer]
public sealed class OrchestrationReference
{
    [Id(0)] public required string Id { get; init; }
    [Id(1)] public required string Name { get; init; }
    [Id(2)] public string? Description { get; init; }
    [Id(3)] public string? ToolName { get; init; }
    [Id(4)] public string? VersionId { get; init; }
}
