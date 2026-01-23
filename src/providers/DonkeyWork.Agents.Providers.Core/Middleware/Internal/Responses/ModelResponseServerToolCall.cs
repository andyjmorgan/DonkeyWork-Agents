namespace DonkeyWork.Agents.Providers.Core.Middleware.Internal.Responses;

internal class ModelResponseServerToolCall : ModelResponseBase
{
    public required string CallId { get; set; }
    public required string ToolName { get; set; }
    public string? Arguments { get; set; }
}
