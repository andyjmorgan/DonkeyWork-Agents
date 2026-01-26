namespace DonkeyWork.Agents.Providers.Contracts.Attributes;

/// <summary>
/// Configures a property to be rendered as a select/dropdown control.
/// Used with enum properties to provide a list of options.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class SelectAttribute : Attribute
{
    /// <summary>
    /// The default value for the select control as a string (use enum name).
    /// </summary>
    public string? DefaultValue { get; init; }
}
