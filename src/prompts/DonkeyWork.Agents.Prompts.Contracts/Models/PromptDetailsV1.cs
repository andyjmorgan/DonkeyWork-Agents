using DonkeyWork.Agents.Prompts.Contracts.Enums;

namespace DonkeyWork.Agents.Prompts.Contracts.Models;

public class PromptDetailsV1
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Content { get; set; } = string.Empty;
    public PromptType PromptType { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
