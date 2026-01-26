namespace DonkeyWork.Agents.Providers.Contracts.Attributes;

/// <summary>
/// Configures a property to be rendered as a slider control with range constraints.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class SliderAttribute : Attribute
{
    /// <summary>
    /// The minimum value for the slider.
    /// </summary>
    public required double Min { get; init; }

    /// <summary>
    /// The maximum value for the slider.
    /// </summary>
    public required double Max { get; init; }

    /// <summary>
    /// The step increment for the slider.
    /// </summary>
    public double Step { get; init; } = 0.1;

    /// <summary>
    /// The default value for the slider. Use double.NaN to indicate no default.
    /// </summary>
    public double DefaultValue { get; init; } = double.NaN;

    /// <summary>
    /// Returns true if a default value was specified.
    /// </summary>
    public bool HasDefaultValue => !double.IsNaN(DefaultValue);
}
