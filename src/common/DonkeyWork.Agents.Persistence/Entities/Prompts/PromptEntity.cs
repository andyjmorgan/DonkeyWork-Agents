namespace DonkeyWork.Agents.Persistence.Entities.Prompts;

public class PromptEntity : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Content { get; set; } = string.Empty;
    public string PromptType { get; set; } = "User";
}
