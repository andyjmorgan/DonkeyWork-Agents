namespace DonkeyWork.Agents.Orleans.Contracts.Contracts;

[GenerateSerializer]
public sealed class WebSearchConfig
{
    [Id(0)] public bool Enabled { get; init; }
    [Id(1)] public int MaxUses { get; init; }
}
