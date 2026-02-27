namespace DonkeyWork.Agents.Actors.Core.Options;

public sealed class AnthropicOptions
{
    public required string ApiKey { get; set; }
    public string DefaultModelId { get; set; } = "claude-sonnet-4-5-20250929";
}
