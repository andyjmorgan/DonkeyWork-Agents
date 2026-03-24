namespace DonkeyWork.Agents.Actors.Core.Providers.Responses;

internal class ModelResponseCompactionContent : ModelResponseBase
{
    public int BlockIndex { get; set; }
    public string? Summary { get; set; }
}
