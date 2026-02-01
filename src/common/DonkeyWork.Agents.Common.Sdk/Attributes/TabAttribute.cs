namespace DonkeyWork.Agents.Common.Sdk.Attributes;

/// <summary>
/// Groups a field into a specific tab in the UI.
/// Tabs provide a way to organize complex configurations into logical sections.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class TabAttribute : Attribute
{
    /// <summary>
    /// The name of the tab this field belongs to.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Display order of the tab (lower values appear first).
    /// </summary>
    public int Order { get; init; } = 100;

    /// <summary>
    /// Optional icon name for the tab (e.g., "settings", "brain").
    /// </summary>
    public string? Icon { get; init; }

    public TabAttribute(string name)
    {
        Name = name;
    }
}
