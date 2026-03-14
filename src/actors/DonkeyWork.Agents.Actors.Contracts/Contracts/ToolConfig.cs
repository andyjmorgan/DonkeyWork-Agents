namespace DonkeyWork.Agents.Actors.Contracts.Contracts;

[GenerateSerializer]
public sealed class ToolConfig
{
    [Id(0)] public bool DeferToolLoading { get; init; } = true;
    [Id(1)] public ToolOverride[] ToolOverrides { get; init; } = [];
}
