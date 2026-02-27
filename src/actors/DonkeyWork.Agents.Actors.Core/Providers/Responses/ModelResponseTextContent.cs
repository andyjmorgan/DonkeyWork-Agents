namespace DonkeyWork.Agents.Actors.Core.Providers.Responses;

internal class ModelResponseTextContent : ModelResponseBase
{
    public int BlockIndex { get; set; }
    public required string Content { get; set; }
}
