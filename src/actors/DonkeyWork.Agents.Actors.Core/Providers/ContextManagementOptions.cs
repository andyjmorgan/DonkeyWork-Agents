namespace DonkeyWork.Agents.Actors.Core.Providers;

internal record ContextManagementOptions
{
    // Compaction: summarize old context when approaching threshold
    public bool CompactionEnabled { get; init; }
    public int CompactionTriggerTokens { get; init; } = 150_000;

    // Tool result clearing: strip old tool results to reduce context
    public bool ClearToolResultsEnabled { get; init; }
    public int ClearToolResultsTriggerTokens { get; init; } = 100_000;
    public int ClearToolResultsKeep { get; init; } = 5;

    // Thinking block clearing: strip old thinking blocks
    public bool ClearThinkingEnabled { get; init; }
    public int ClearThinkingKeepTurns { get; init; } = 2;
}
