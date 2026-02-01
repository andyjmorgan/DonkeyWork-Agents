using DonkeyWork.Agents.Common.Nodes.Enums;

namespace DonkeyWork.Agents.Common.Nodes.Providers;

/// <summary>
/// Marks a method as a node execution handler for a specific node type.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class NodeMethodAttribute : Attribute
{
    /// <summary>
    /// The node type this method handles.
    /// </summary>
    public NodeType NodeType { get; }

    public NodeMethodAttribute(NodeType nodeType)
    {
        NodeType = nodeType;
    }
}
