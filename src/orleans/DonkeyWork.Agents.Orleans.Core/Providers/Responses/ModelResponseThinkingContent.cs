namespace DonkeyWork.Agents.Orleans.Core.Providers.Responses;

internal class ModelResponseThinkingContent : ModelResponseBase
{
    public int Index { get; set; }
    public string Content { get; set; } = "";
    public string? Signature { get; set; }
    public bool IsEncrypted { get; set; }
    public Dictionary<string, string> Properties { get; set; } = new();
}
