using System.Text.Json;

namespace DonkeyWork.Agents.Persistence.Entities.AgentDefinitions;

public class AgentDefinitionEntity : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystem { get; set; }
    public JsonDocument Contract { get; set; } = JsonDocument.Parse("{}");
    public JsonDocument? ReactFlowData { get; set; }
    public JsonDocument? NodeConfigurations { get; set; }
    public bool ConnectToNavi { get; set; }
    public string? Icon { get; set; }
}
