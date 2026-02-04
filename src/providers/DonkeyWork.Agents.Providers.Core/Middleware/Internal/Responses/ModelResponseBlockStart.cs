namespace DonkeyWork.Agents.Providers.Core.Middleware.Internal.Responses;

internal class ModelResponseBlockStart : ModelResponseBase
{
    public required int BlockIndex { get; set; }
    public required InternalContentBlockType Type { get; set; }
}

internal enum InternalContentBlockType
{
    Text,
    Thinking,
    Image,
    ToolUse,
    ToolResult
}
