namespace DonkeyWork.Agents.Common.Sdk.Attributes;

/// <summary>
/// Marks a field as requiring a specific model capability to be available.
/// The field will only be shown if the selected model supports the required capability.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public sealed class RequiresCapabilityAttribute : Attribute
{
    /// <summary>
    /// The name of the capability required (e.g., "Reasoning", "Vision", "FunctionCalling").
    /// This should match a property name on the ModelSupports class.
    /// </summary>
    public required string Capability { get; init; }
}
