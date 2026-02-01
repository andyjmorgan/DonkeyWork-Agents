using DonkeyWork.Agents.Common.Nodes.Enums;

namespace DonkeyWork.Agents.Common.Nodes.Schema;

/// <summary>
/// Generates configuration schemas from node configuration types.
/// </summary>
public interface INodeSchemaGenerator
{
    /// <summary>
    /// Generates a schema for the specified node type.
    /// </summary>
    NodeConfigSchema GenerateSchema(NodeType nodeType);

    /// <summary>
    /// Generates a schema for the specified configuration type.
    /// </summary>
    NodeConfigSchema GenerateSchema<TConfig>() where TConfig : Configurations.NodeConfiguration;

    /// <summary>
    /// Gets schemas for all registered node types.
    /// </summary>
    IReadOnlyDictionary<NodeType, NodeConfigSchema> GetAllSchemas();
}
