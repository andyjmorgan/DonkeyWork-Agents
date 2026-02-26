namespace DonkeyWork.Agents.Orleans.Core.Middleware.Messages;

internal class ToolResponseMessage : BaseMiddlewareMessage
{
    public required string ToolCallId { get; init; }
    public required string ToolName { get; init; }
    public required string Response { get; init; }
    public required TimeSpan Duration { get; init; }
    public required bool Success { get; init; }
}
