namespace DonkeyWork.Agents.Actors.Contracts.Contracts;

[GenerateSerializer]
public sealed class ToolOverride
{
    [Id(0)] public required string Source { get; init; }
    [Id(1)] public required string ToolName { get; init; }
    [Id(2)] public bool Enabled { get; init; } = true;
    [Id(3)] public bool? Deferred { get; init; }
}
