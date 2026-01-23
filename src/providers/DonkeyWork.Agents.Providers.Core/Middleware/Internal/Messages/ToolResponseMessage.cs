namespace DonkeyWork.Agents.Providers.Core.Middleware.Internal.Messages;

/// <summary>
/// Tool execution result message.
/// </summary>
internal class ToolResponseMessage : BaseMiddlewareMessage
{
    public required string CallId { get; set; }
    public required string ToolName { get; set; }
    public required string Response { get; set; }
    public bool Success { get; set; } = true;
}
