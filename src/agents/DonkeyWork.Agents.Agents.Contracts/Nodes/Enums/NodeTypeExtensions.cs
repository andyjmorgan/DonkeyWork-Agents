namespace DonkeyWork.Agents.Agents.Contracts.Nodes.Enums;

/// <summary>
/// Extension methods for NodeType enum.
/// </summary>
public static class NodeTypeExtensions
{
    /// <summary>
    /// Maps a ReactFlow node type string (lowercase) to the NodeType enum.
    /// </summary>
    /// <param name="reactFlowType">The ReactFlow node type string (e.g., "start", "model").</param>
    /// <returns>The corresponding NodeType enum value.</returns>
    /// <exception cref="ArgumentException">If the node type is not recognized.</exception>
    public static NodeType ToNodeType(this string reactFlowType)
    {
        return reactFlowType switch
        {
            "start" => NodeType.Start,
            "end" => NodeType.End,
            "model" => NodeType.Model,
            "messageFormatter" => NodeType.MessageFormatter,
            "httpRequest" => NodeType.HttpRequest,
            "sleep" => NodeType.Sleep,
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
            NodeType.Start => "Start",
            NodeType.End => "End",
            NodeType.Model => "Model",
            NodeType.MessageFormatter => "MessageFormatter",
            NodeType.HttpRequest => "HttpRequest",
            NodeType.Sleep => "Sleep",
            _ => throw new ArgumentException($"Unknown node type: {nodeType}", nameof(nodeType))
        };
    }
}
