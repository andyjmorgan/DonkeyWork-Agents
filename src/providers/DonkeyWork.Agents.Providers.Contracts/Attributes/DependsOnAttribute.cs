namespace DonkeyWork.Agents.Providers.Contracts.Attributes;

/// <summary>
/// Marks a property as conditionally visible based on another field's value.
/// The field will only be shown in the UI if the dependency condition is met.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public sealed class DependsOnAttribute : Attribute
{
    /// <summary>
    /// The name of the field this property depends on (use property name, not camelCase).
    /// </summary>
    public required string FieldName { get; init; }

    /// <summary>
    /// The value the dependency field must have for this field to be visible.
    /// For boolean fields, use true/false. For enums, use the enum value.
    /// </summary>
    public required object Value { get; init; }
}
