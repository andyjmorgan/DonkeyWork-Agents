namespace DonkeyWork.Agents.Actors.Core.Providers.Responses;

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
