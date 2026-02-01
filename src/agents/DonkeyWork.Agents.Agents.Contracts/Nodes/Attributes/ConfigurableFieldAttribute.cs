using DonkeyWork.Agents.Agents.Contracts.Nodes.Enums;

namespace DonkeyWork.Agents.Agents.Contracts.Nodes.Attributes;

/// <summary>
/// Marks a property as a configurable field and provides metadata for schema generation.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class ConfigurableFieldAttribute : Attribute
{
    /// <summary>
    /// The display label for the field in the UI.
    /// </summary>
    public required string Label { get; init; }

    /// <summary>
    /// The UI control type for this field. If not specified, determined from property type.
    /// </summary>
    public ControlType ControlType { get; init; } = ControlType.Text;

    /// <summary>
    /// Display order within the form (lower values appear first).
    /// </summary>
    public int Order { get; init; } = 100;

    /// <summary>
    /// Whether this field is required. Default is false (determined from nullability if not set).
    /// </summary>
    public bool Required { get; init; } = false;

    /// <summary>
    /// Placeholder text for the field input.
    /// </summary>
    public string? Placeholder { get; init; }

    /// <summary>
    /// Optional description explaining the field's purpose.
    /// </summary>
    public string? Description { get; init; }
}
