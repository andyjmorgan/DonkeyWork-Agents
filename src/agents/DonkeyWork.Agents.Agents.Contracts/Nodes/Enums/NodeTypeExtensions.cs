namespace DonkeyWork.Agents.Agents.Contracts.Nodes.Enums;

/// <summary>
/// Extension methods for NodeType enum.
/// </summary>
public static class NodeTypeExtensions
{
    /// <summary>
    /// Maps a ReactFlow node type string to the NodeType enum.
    /// Handles both old format (lowercase) and new format (PascalCase from data.nodeType).
    /// </summary>
    /// <param name="reactFlowType">The ReactFlow node type string (e.g., "start", "Start", "MultimodalChatModel").</param>
    /// <returns>The corresponding NodeType enum value.</returns>
    /// <exception cref="ArgumentException">If the node type is not recognized.</exception>
    public static NodeType ToNodeType(this string reactFlowType)
    {
        return reactFlowType switch
        {
            // Old format (lowercase)
            "start" => NodeType.Start,
            "end" => NodeType.End,
            "model" => NodeType.Model,
            "messageFormatter" => NodeType.MessageFormatter,
            "httpRequest" => NodeType.HttpRequest,
            "sleep" => NodeType.Sleep,
            // New format (PascalCase - from data.nodeType)
            nameof(NodeType.Start) => NodeType.Start,
            nameof(NodeType.End) => NodeType.End,
            nameof(NodeType.Model) => NodeType.Model,
            nameof(NodeType.MultimodalChatModel) => NodeType.MultimodalChatModel,
            nameof(NodeType.MessageFormatter) => NodeType.MessageFormatter,
            nameof(NodeType.HttpRequest) => NodeType.HttpRequest,
            nameof(NodeType.Sleep) => NodeType.Sleep,
            _ => throw new ArgumentException($"Unknown ReactFlow node type: {reactFlowType}", nameof(reactFlowType))
        };
    }

    /// <summary>
    /// Gets the ReactFlow node type string for a NodeType enum value.
    /// </summary>
    /// <param name="nodeType">The NodeType enum value.</param>
    /// <returns>The ReactFlow node type string.</returns>
    public static string ToReactFlowType(this NodeType nodeType)
    {
        return nodeType switch
        {
            NodeType.Start => "start",
            NodeType.End => "end",
            NodeType.Model => "model",
            NodeType.MessageFormatter => "messageFormatter",
            NodeType.HttpRequest => "httpRequest",
            NodeType.Sleep => "sleep",
            _ => throw new ArgumentException($"Unknown node type: {nodeType}", nameof(nodeType))
        };
    }

    /// <summary>
    /// Gets the JSON type discriminator for polymorphic serialization.
    /// </summary>
    /// <param name="nodeType">The NodeType enum value.</param>
    /// <returns>The JSON type discriminator string.</returns>
    public static string ToTypeDiscriminator(this NodeType nodeType)
    {
        return nodeType switch
        {
            NodeType.Start => nameof(NodeType.Start),
            NodeType.End => nameof(NodeType.End),
            NodeType.Model => nameof(NodeType.Model),
            NodeType.MultimodalChatModel => nameof(NodeType.MultimodalChatModel),
            NodeType.MessageFormatter => nameof(NodeType.MessageFormatter),
            NodeType.HttpRequest => nameof(NodeType.HttpRequest),
            NodeType.Sleep => nameof(NodeType.Sleep),
            _ => throw new ArgumentException($"Unknown node type: {nodeType}", nameof(nodeType))
        };
    }
}
