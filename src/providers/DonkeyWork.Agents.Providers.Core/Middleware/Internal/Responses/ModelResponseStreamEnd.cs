namespace DonkeyWork.Agents.Providers.Core.Middleware.Internal.Responses;

internal class ModelResponseStreamEnd : ModelResponseBase
{
    public required InternalStopReason Reason { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

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
