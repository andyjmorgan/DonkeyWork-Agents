namespace DonkeyWork.Agents.Providers.Core.Middleware.Internal.Responses;

internal class ModelResponseEncryptedThinkingContent : ModelResponseBase
{
    public required string EncryptedContent { get; set; }
    public string? Signature { get; set; }
}
