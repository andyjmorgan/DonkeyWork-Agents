namespace DonkeyWork.Agents.Providers.Core.Middleware.Internal.Responses;

internal class ModelResponseUsage : ModelResponseBase
{
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
}
