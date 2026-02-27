namespace DonkeyWork.Agents.Actors.Core.Providers.Responses;

internal class ModelResponseUsage : ModelResponseBase
{
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public int CachedInputTokens { get; set; }
    public int WebSearchRequests { get; set; }
    public int WebFetchRequests { get; set; }
}
