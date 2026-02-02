using DonkeyWork.Agents.Orchestrations.Contracts.Models;

namespace DonkeyWork.Agents.Orchestrations.Contracts.Services;

/// <summary>
/// Service for retrieving node type definitions and their configuration schemas.
/// </summary>
public interface INodeTypeSchemaService
{
    /// <summary>
    /// Gets all available node types with their configuration schemas.
    /// </summary>
    IReadOnlyList<NodeTypeInfo> GetNodeTypes();
}
