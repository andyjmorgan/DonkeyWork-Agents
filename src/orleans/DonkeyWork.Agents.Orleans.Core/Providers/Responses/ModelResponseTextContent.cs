namespace DonkeyWork.Agents.Orleans.Core.Providers.Responses;

internal class ModelResponseTextContent : ModelResponseBase
{
    public int BlockIndex { get; set; }
    public required string Content { get; set; }
}
