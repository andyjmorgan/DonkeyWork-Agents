namespace DonkeyWork.Agents.Providers.Contracts.Attributes;

/// <summary>
/// Marks a property as a configurable field and provides metadata for schema generation.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class ConfigFieldAttribute : Attribute
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
    /// Optional group name for organizing related fields.
    /// </summary>
    public string? Group { get; init; }
}
