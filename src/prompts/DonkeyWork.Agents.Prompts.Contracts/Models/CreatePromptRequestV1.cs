using DonkeyWork.Agents.Prompts.Contracts.Enums;

namespace DonkeyWork.Agents.Prompts.Contracts.Models;

public class CreatePromptRequestV1
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Content { get; set; } = string.Empty;
    public PromptType PromptType { get; set; } = PromptType.User;
}
