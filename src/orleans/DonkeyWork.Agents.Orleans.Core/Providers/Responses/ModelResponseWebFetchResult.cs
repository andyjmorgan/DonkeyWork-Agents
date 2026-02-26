namespace DonkeyWork.Agents.Orleans.Core.Providers.Responses;

internal class ModelResponseWebFetchResult : ModelResponseBase
{
    public required int BlockIndex { get; set; }
    public required string ToolUseId { get; set; }
    public required string RawJson { get; set; }
}
