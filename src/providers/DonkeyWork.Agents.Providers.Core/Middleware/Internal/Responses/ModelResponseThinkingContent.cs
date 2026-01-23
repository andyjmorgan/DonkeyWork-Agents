namespace DonkeyWork.Agents.Providers.Core.Middleware.Internal.Responses;

internal class ModelResponseThinkingContent : ModelResponseBase
{
    public required string Content { get; set; }
    public string? Signature { get; set; }
}
