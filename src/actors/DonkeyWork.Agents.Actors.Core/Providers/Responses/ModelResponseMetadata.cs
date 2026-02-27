namespace DonkeyWork.Agents.Actors.Core.Providers.Responses;

internal class ModelResponseMetadata : ModelResponseBase
{
    public InternalStopReason StopReason { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new();
}
