using DonkeyWork.Agents.Actions.Contracts.Models.Schema;

namespace DonkeyWork.Agents.Actions.Contracts.Services;

/// <summary>
/// Service for generating action node schemas from C# types
/// </summary>
public interface IActionSchemaService
{
    /// <summary>
    /// Generate schemas for all action nodes in an assembly
    /// </summary>
    /// <param name="assembly">Assembly to scan for action nodes</param>
    /// <returns>List of action node schemas</returns>
    List<ActionNodeSchema> GenerateSchemas(System.Reflection.Assembly assembly);

    /// <summary>
    /// Generate schema for a specific action parameter type
    /// </summary>
    /// <param name="parameterType">Type decorated with [ActionNode]</param>
    /// <returns>Action node schema</returns>
    ActionNodeSchema GenerateSchema(Type parameterType);

    /// <summary>
    /// Export schemas as JSON string
    /// </summary>
    /// <param name="schemas">List of schemas to export</param>
    /// <returns>JSON representation</returns>
    string ExportAsJson(List<ActionNodeSchema> schemas);
}
