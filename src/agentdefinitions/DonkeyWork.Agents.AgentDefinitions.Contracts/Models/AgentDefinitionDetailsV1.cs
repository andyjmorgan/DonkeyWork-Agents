using System.Text.Json;

namespace DonkeyWork.Agents.AgentDefinitions.Contracts.Models;

public class AgentDefinitionDetailsV1
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystem { get; set; }
    public bool ConnectToNavi { get; set; }
    public JsonElement Contract { get; set; }
    public JsonElement? ReactFlowData { get; set; }
    public JsonElement? NodeConfigurations { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
