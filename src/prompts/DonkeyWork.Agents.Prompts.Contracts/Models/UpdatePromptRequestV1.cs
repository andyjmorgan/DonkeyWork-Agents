using DonkeyWork.Agents.Prompts.Contracts.Enums;

namespace DonkeyWork.Agents.Prompts.Contracts.Models;

public class UpdatePromptRequestV1
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Content { get; set; }
    public PromptType? PromptType { get; set; }
}
