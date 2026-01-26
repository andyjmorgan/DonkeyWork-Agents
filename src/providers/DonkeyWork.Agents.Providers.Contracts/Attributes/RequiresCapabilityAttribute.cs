namespace DonkeyWork.Agents.Providers.Contracts.Attributes;

/// <summary>
/// Marks a property as requiring a specific model capability to be available.
/// The field will only be included in the schema if the model supports the capability.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public sealed class RequiresCapabilityAttribute : Attribute
{
    /// <summary>
    /// The capability name that maps to a property on ModelSupports (e.g., "Reasoning", "Vision").
    /// </summary>
    public required string Capability { get; init; }
}
