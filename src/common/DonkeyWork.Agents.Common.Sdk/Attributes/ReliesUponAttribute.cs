namespace DonkeyWork.Agents.Common.Sdk.Attributes;

/// <summary>
/// Marks a field as relying upon another field's value.
/// The field will only be shown/enabled when the parent field has the specified value.
/// This is used for creating clusters of related settings that depend on a toggle.
/// </summary>
/// <example>
/// <code>
/// [ConfigurableField(Label = "Enable Reasoning")]
/// public Resolvable&lt;bool&gt;? EnableReasoning { get; init; }
///
/// [ConfigurableField(Label = "Reasoning Effort")]
/// [ReliesUpon(nameof(EnableReasoning), Value = true)]
/// public Resolvable&lt;ReasoningEffort&gt;? ReasoningEffort { get; init; }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public sealed class ReliesUponAttribute : Attribute
{
    /// <summary>
    /// The name of the field this property relies upon (use property name, not camelCase).
    /// </summary>
    public required string FieldName { get; init; }

    /// <summary>
    /// The value the parent field must have for this field to be shown/enabled.
    /// For boolean fields, use true/false. For enums, use the enum value.
    /// </summary>
    public required object Value { get; init; }

    /// <summary>
    /// When the parent field has the required value, is this field required?
    /// Default is false - the field becomes visible but optional.
    /// </summary>
    public bool RequiredWhenEnabled { get; init; } = false;
}
