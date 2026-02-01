namespace DonkeyWork.Agents.Common.Sdk.Attributes;

/// <summary>
/// Configures a property to be rendered as a dropdown/select control.
/// For enum types, options are automatically derived from the enum values.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class SelectOptionsAttribute : Attribute
{
    /// <summary>
    /// The default selected value.
    /// </summary>
    public string? Default { get; init; }

    /// <summary>
    /// Explicit options to display. If not provided and the property type is an enum,
    /// options will be derived from the enum values.
    /// </summary>
    public string[]? Options { get; init; }

    /// <summary>
    /// Whether to allow multiple selections. Default is false.
    /// </summary>
    public bool AllowMultiple { get; init; } = false;
}
