namespace DonkeyWork.Agents.Providers.Core.Middleware.Internal.Messages;

/// <summary>
/// Tool call request message.
/// </summary>
internal class ToolRequestMessage : BaseMiddlewareMessage
{
    public required string CallId { get; set; }
    public required string ToolName { get; set; }
    public required string Arguments { get; set; }
}
