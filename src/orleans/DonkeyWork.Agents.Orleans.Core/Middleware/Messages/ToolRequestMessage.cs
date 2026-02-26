using System.Text.Json;

namespace DonkeyWork.Agents.Orleans.Core.Middleware.Messages;

internal class ToolRequestMessage : BaseMiddlewareMessage
{
    public required string ToolCallId { get; init; }
    public required string ToolName { get; init; }
    public required JsonElement Arguments { get; init; }
}
