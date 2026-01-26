namespace DonkeyWork.Agents.Providers.Contracts.Attributes;

/// <summary>
/// Configures a property to be rendered as a number input with range constraints.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class RangeConstraintAttribute : Attribute
{
    /// <summary>
    /// Sentinel value indicating no default was specified.
    /// </summary>
    public const int NoDefault = int.MinValue;

    /// <summary>
    /// The minimum value allowed.
    /// </summary>
    public required int Min { get; init; }

    /// <summary>
    /// The maximum value allowed.
    /// </summary>
    public required int Max { get; init; }

    /// <summary>
    /// The default value for the input. Use int.MinValue to indicate no default.
    /// </summary>
    public int DefaultValue { get; init; } = NoDefault;

    /// <summary>
    /// Returns true if a default value was specified.
    /// </summary>
    public bool HasDefaultValue => DefaultValue != NoDefault;
}
