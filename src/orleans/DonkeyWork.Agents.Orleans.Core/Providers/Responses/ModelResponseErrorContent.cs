namespace DonkeyWork.Agents.Orleans.Core.Providers.Responses;

internal class ModelResponseErrorContent : ModelResponseBase
{
    public required string ErrorMessage { get; set; }
    public Exception? Exception { get; set; }
}
