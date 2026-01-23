namespace DonkeyWork.Agents.Providers.Core.Middleware.Internal.Responses;

internal class ModelResponseToolCall : ModelResponseBase
{
    public required string CallId { get; set; }
    public required string ToolName { get; set; }
    public required string Arguments { get; set; }
}
