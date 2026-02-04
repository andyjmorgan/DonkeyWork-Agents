namespace DonkeyWork.Agents.Providers.Core.Middleware.Internal.Responses;

internal class ModelResponseTextContent : ModelResponseBase
{
    public int BlockIndex { get; set; }

    public required string Content { get; set; }
}
