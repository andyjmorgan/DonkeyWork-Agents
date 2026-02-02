namespace DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Attributes;

/// <summary>
/// Assigns a field to a named tab in the configuration UI.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class TabAttribute : Attribute
{
    /// <summary>
    /// The name of the tab.
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
