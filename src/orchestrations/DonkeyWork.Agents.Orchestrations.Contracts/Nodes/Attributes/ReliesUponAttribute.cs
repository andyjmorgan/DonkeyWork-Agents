namespace DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Attributes;

/// <summary>
/// Specifies that a field's visibility depends on another field's value.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class ReliesUponAttribute : Attribute
{
    /// <summary>
    /// The name of the field this depends on.
    /// </summary>
    public required string FieldName { get; init; }

    /// <summary>
    /// The value that the dependent field must have for this field to be visible.
    /// </summary>
    public required object Value { get; init; }

    /// <summary>
    /// Whether this field becomes required when enabled.
    /// </summary>
    public bool RequiredWhenEnabled { get; init; } = false;
}
