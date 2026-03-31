using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Orchestrations.Contracts.Models.Interfaces;

/// <summary>
/// Base configuration for an orchestration interface.
/// An orchestration can support multiple interface types simultaneously.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(DirectInterfaceConfig), nameof(DirectInterfaceConfig))]
[JsonDerivedType(typeof(ToolInterfaceConfig), nameof(ToolInterfaceConfig))]
[JsonDerivedType(typeof(McpInterfaceConfig), nameof(McpInterfaceConfig))]
[JsonDerivedType(typeof(ChatInterfaceConfig), nameof(ChatInterfaceConfig))]
[JsonDerivedType(typeof(A2aInterfaceConfig), nameof(A2aInterfaceConfig))]
[JsonDerivedType(typeof(WebhookInterfaceConfig), nameof(WebhookInterfaceConfig))]
public abstract class InterfaceConfig
{
    /// <summary>
    /// Display name for this interface instance.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Description of what this interface does.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }
}
