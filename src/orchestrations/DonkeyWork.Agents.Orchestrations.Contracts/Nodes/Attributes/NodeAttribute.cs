namespace DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Attributes;

/// <summary>
/// Defines all metadata for a node type - display properties and behavior.
/// Applied to NodeConfiguration classes. The schema generator reads this to build NodeTypeInfo.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class NodeAttribute : Attribute
{
    /// <summary>
    /// Display name shown in the palette and on the node.
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// Description of what the node does.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Category for grouping in the palette (e.g., "Flow", "AI", "Utility", "Integration").
    /// </summary>
    public required string Category { get; init; }

    /// <summary>
    /// Icon name (lucide-react icon name, e.g., "play", "brain", "globe").
    /// </summary>
    public string? Icon { get; init; }

    /// <summary>
    /// Color theme for the node (e.g., "green", "blue", "purple", "cyan", "orange").
    /// </summary>
    public string? Color { get; init; }

    /// <summary>
    /// Whether this node has an input handle (can receive connections). Default: true.
    /// </summary>
    public bool HasInputHandle { get; init; } = true;

    /// <summary>
    /// Whether this node has an output handle (can send connections). Default: true.
    /// </summary>
    public bool HasOutputHandle { get; init; } = true;

    /// <summary>
    /// Whether this node can be deleted by the user. Default: true.
    /// </summary>
    public bool CanDelete { get; init; } = true;
}
