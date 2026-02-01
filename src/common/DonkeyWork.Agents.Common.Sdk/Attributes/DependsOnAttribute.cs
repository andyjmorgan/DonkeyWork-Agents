namespace DonkeyWork.Agents.Common.Sdk.Attributes;

/// <summary>
/// Makes a parameter's visibility depend on another parameter's value.
/// This is a simpler form of conditional visibility compared to ReliesUpon.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public sealed class DependsOnAttribute : Attribute
{
    /// <summary>
    /// Name of the parameter this depends on (use property name, not camelCase).
    /// </summary>
    public string ParameterName { get; }

    /// <summary>
    /// Condition for showing this parameter (e.g., "Type == 'custom'").
    /// </summary>
    public string? ShowIf { get; init; }

    public DependsOnAttribute(string parameterName)
    {
        ParameterName = parameterName;
    }
}
