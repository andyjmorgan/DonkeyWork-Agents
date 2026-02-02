namespace DonkeyWork.Agents.Agents.Contracts.Nodes.Attributes;

/// <summary>
/// Defines options for Select control type.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class SelectOptionsAttribute : Attribute
{
    /// <summary>
    /// The available options for selection.
    /// </summary>
    public string[] Options { get; }

    /// <summary>
    /// The default selected option.
    /// </summary>
    public string? Default { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SelectOptionsAttribute"/> class.
    /// </summary>
    /// <param name="options">The available options for selection.</param>
    public SelectOptionsAttribute(params string[] options)
    {
        Options = options;
    }
}
