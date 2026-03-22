using System.Text.Json;

namespace DonkeyWork.Agents.AgentDefinitions.Contracts.Models;

public class NaviAgentDefinitionV1
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public JsonElement Contract { get; set; }
}
