namespace DonkeyWork.Agents.AgentDefinitions.Contracts.Models;

public class AgentDefinitionSummaryV1
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystem { get; set; }
    public bool ConnectToNavi { get; set; }
    public string? Icon { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
