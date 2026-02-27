using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Enums;

namespace DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Configurations;

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
[JsonDerivedType(typeof(MultimodalChatModelNodeConfiguration), typeDiscriminator: "MultimodalChatModel")]
public abstract class NodeConfiguration
{
    /// <summary>
    /// The unique name of this node instance (used for template variable references).
    /// </summary>
    [JsonPropertyName("name")]
    [Required]
    public required string Name { get; init; }

    /// <summary>
    /// The type of this node.
    /// </summary>
    [JsonIgnore]
    public abstract NodeType NodeType { get; }

    /// <summary>
    /// Validates the configuration using DataAnnotations.
    /// Override in derived classes to add type-specific validation (call base.Validate() first).
    /// </summary>
    /// <exception cref="ValidationException">Thrown when validation fails.</exception>
    public virtual void Validate()
    {
        var context = new ValidationContext(this);
        var results = new List<ValidationResult>();

        if (!Validator.TryValidateObject(this, context, results, validateAllProperties: true))
        {
            var errors = string.Join("; ", results.Select(r => r.ErrorMessage));
            throw new ValidationException($"Configuration validation failed: {errors}");
        }
    }
}
