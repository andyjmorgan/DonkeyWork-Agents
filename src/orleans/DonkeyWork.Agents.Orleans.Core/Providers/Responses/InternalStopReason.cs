namespace DonkeyWork.Agents.Orleans.Core.Providers.Responses;

internal enum InternalStopReason
{
    EndTurn,
    ToolUse,
    MaxTokens,
    Incomplete,
    ContentFilter,
    SafetyStop,
    Recitation,
    Cancelled
}
