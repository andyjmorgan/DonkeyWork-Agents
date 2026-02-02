namespace DonkeyWork.Agents.Agents.Contracts.Nodes.Attributes;

/// <summary>
/// Groups fields visually within a tab.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class GroupAttribute : Attribute
{
    /// <summary>
    /// The name of the group.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Display order of the group (lower values appear first).
    /// </summary>
    public int Order { get; init; } = 100;

    /// <summary>
    /// Initializes a new instance of the <see cref="GroupAttribute"/> class.
    /// </summary>
    /// <param name="name">The name of the group.</param>
    public GroupAttribute(string name)
    {
        Name = name;
    }
}
