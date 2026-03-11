using System.Text.Json;

namespace DonkeyWork.Agents.AgentDefinitions.Contracts.Models;

public class UpdateAgentDefinitionRequestV1
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public bool? ConnectToNavi { get; set; }
    public JsonElement? Contract { get; set; }
    public JsonElement? ReactFlowData { get; set; }
    public JsonElement? NodeConfigurations { get; set; }
}
