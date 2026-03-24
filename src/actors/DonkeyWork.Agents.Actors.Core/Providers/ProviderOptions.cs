using DonkeyWork.Agents.Common.Contracts.Enums;

namespace DonkeyWork.Agents.Actors.Core.Providers;

internal record WebSearchOptions
{
    public bool Enabled { get; init; }
    public int? MaxUses { get; init; }
}

internal record WebFetchOptions
{
    public bool Enabled { get; init; }
    public int? MaxUses { get; init; }
}

internal record ProviderOptions
{
    public required string ApiKey { get; init; }
    public required string ModelId { get; init; }
    public long MaxTokens { get; init; } = 20_000;
    public int? ThinkingBudgetTokens { get; init; }
    public ReasoningEffort? ReasoningEffort { get; init; }
    public WebSearchOptions WebSearch { get; init; } = new();
    public WebFetchOptions WebFetch { get; init; } = new();
    public bool Stream { get; init; } = true;
    public ContextManagementOptions ContextManagement { get; init; } = new();
}
