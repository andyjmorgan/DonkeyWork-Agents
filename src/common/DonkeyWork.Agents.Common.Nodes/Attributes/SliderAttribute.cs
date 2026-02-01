namespace DonkeyWork.Agents.Common.Nodes.Attributes;

/// <summary>
/// Configures slider control properties for numeric fields.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class SliderAttribute : Attribute
{
    private const double NoDefault = double.NaN;

    /// <summary>
    /// Minimum value.
    /// </summary>
    public double Min { get; init; }

    /// <summary>
    /// Maximum value.
    /// </summary>
    public double Max { get; init; }

    /// <summary>
    /// Step increment.
    /// </summary>
    public double Step { get; init; } = 1;

    /// <summary>
    /// Default value (use double.NaN for no default).
    /// </summary>
    public double Default { get; init; } = NoDefault;

    /// <summary>
    /// Whether a default value has been set.
    /// </summary>
    public bool HasDefault => !double.IsNaN(Default);
}
