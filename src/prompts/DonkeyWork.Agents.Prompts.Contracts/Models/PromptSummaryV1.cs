using DonkeyWork.Agents.Prompts.Contracts.Enums;

namespace DonkeyWork.Agents.Prompts.Contracts.Models;

public class PromptSummaryV1
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public PromptType PromptType { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
