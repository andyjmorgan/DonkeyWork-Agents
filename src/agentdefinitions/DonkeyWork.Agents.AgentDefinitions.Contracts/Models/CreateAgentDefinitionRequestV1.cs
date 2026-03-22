namespace DonkeyWork.Agents.AgentDefinitions.Contracts.Models;

public class CreateAgentDefinitionRequestV1
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
}
