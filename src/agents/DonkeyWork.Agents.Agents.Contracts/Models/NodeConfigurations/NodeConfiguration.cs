using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Agents.Contracts.Models.NodeConfigurations;

/// <summary>
/// Base class for node configurations.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(StartNodeConfiguration), "start")]
[JsonDerivedType(typeof(ModelNodeConfiguration), "model")]
[JsonDerivedType(typeof(EndNodeConfiguration), "end")]
[JsonDerivedType(typeof(ActionNodeConfiguration), "action")]
public abstract class NodeConfiguration
{
    /// <summary>
    /// Unique node name for template references.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }
}
