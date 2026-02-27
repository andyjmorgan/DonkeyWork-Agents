namespace DonkeyWork.Agents.Actors.Core.Providers.Responses;

internal class ModelResponseCitationContent : ModelResponseBase
{
    public int Index { get; set; }
    public string Title { get; set; } = "";
    public string Url { get; set; } = "";
    public string CitedText { get; set; } = "";
}
