namespace DonkeyWork.Agents.Common.Sdk.Attributes;

/// <summary>
/// Configures a property to be rendered as a number input with range constraints.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class RangeConstraintAttribute : Attribute
{
    /// <summary>
    /// Sentinel value indicating no default was specified.
    /// </summary>
    public const double NoDefault = double.NaN;

    /// <summary>
    /// The minimum value allowed.
    /// </summary>
    public required double Min { get; init; }

    /// <summary>
    /// The maximum value allowed.
    /// </summary>
    public required double Max { get; init; }

    /// <summary>
    /// The default value for the input. Use double.NaN to indicate no default.
    /// </summary>
    public double Default { get; init; } = NoDefault;

    /// <summary>
    /// Returns true if a default value was specified.
    /// </summary>
    public bool HasDefault => !double.IsNaN(Default);
}
