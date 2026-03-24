namespace DonkeyWork.Agents.Actors.Contracts.Contracts;

[GenerateSerializer]
public sealed class ContextManagementConfig
{
    [Id(0)] public bool CompactionEnabled { get; init; }
    [Id(1)] public int CompactionTriggerTokens { get; init; } = 150_000;
    [Id(2)] public bool ClearToolResultsEnabled { get; init; }
    [Id(3)] public int ClearToolResultsTriggerTokens { get; init; } = 100_000;
    [Id(4)] public int ClearToolResultsKeep { get; init; } = 5;
    [Id(5)] public bool ClearThinkingEnabled { get; init; }
    [Id(6)] public int ClearThinkingKeepTurns { get; init; } = 2;
}
