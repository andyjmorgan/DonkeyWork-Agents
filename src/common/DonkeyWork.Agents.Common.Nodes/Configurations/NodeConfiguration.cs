using System.Text.Json.Serialization;
using DonkeyWork.Agents.Common.Nodes.Enums;

namespace DonkeyWork.Agents.Common.Nodes.Configurations;

/// <summary>
/// Base class for all node configurations.
/// Derived types are registered at runtime via NodeConfigurationRegistry for polymorphic serialization.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(StartNodeConfiguration), typeDiscriminator: "Start")]
[JsonDerivedType(typeof(EndNodeConfiguration), typeDiscriminator: "End")]
[JsonDerivedType(typeof(ModelNodeConfiguration), typeDiscriminator: "Model")]
[JsonDerivedType(typeof(MessageFormatterNodeConfiguration), typeDiscriminator: "MessageFormatter")]
[JsonDerivedType(typeof(HttpRequestNodeConfiguration), typeDiscriminator: "HttpRequest")]
[JsonDerivedType(typeof(SleepNodeConfiguration), typeDiscriminator: "Sleep")]
public abstract class NodeConfiguration
{
    /// <summary>
    /// The unique name of this node instance (used for template variable references).
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// The type of this node.
    /// </summary>
    [JsonIgnore]
    public abstract NodeType Type { get; }
}
