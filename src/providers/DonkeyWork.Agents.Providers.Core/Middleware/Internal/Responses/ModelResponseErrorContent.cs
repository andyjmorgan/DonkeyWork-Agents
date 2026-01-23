namespace DonkeyWork.Agents.Providers.Core.Middleware.Internal.Responses;

internal class ModelResponseErrorContent : ModelResponseBase
{
    public required string ErrorMessage { get; set; }
    public Exception? Exception { get; set; }
}
