namespace DonkeyWork.Agents.Common.Sdk.Attributes;

/// <summary>
/// Marks a property as a configurable field and provides metadata for schema generation.
/// This is the unified attribute used by both Actions and Model configurations.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class ConfigurableFieldAttribute : Attribute
{
    /// <summary>
    /// The display label for the field in the UI.
    /// </summary>
    public required string Label { get; init; }

    /// <summary>
    /// Optional description explaining the field's purpose.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Display order within the form (lower values appear first).
    /// </summary>
    public int Order { get; init; } = 100;

    /// <summary>
    /// Optional group name for organizing related fields within a tab.
    /// </summary>
    public string? Group { get; init; }

    /// <summary>
    /// Whether this field is required. Default is false.
    /// </summary>
    public bool Required { get; init; } = false;

    /// <summary>
    /// Placeholder text for the field input.
    /// </summary>
    public string? Placeholder { get; init; }
}
