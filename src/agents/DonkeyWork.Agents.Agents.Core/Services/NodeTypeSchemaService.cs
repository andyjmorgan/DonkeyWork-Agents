using System.Text.Json;
using DonkeyWork.Agents.Agents.Contracts.Models;
using DonkeyWork.Agents.Agents.Contracts.Models.NodeConfigurations;
using DonkeyWork.Agents.Agents.Contracts.Services;
using NJsonSchema;
using NJsonSchema.Generation;

namespace DonkeyWork.Agents.Agents.Core.Services;

public class NodeTypeSchemaService : INodeTypeSchemaService
{
    private readonly Lazy<IReadOnlyList<NodeTypeInfo>> _nodeTypes;

    public NodeTypeSchemaService()
    {
        _nodeTypes = new Lazy<IReadOnlyList<NodeTypeInfo>>(GenerateNodeTypes);
    }

    public IReadOnlyList<NodeTypeInfo> GetNodeTypes()
    {
        return _nodeTypes.Value;
    }

    private static IReadOnlyList<NodeTypeInfo> GenerateNodeTypes()
    {
        var settings = new SystemTextJsonSchemaGeneratorSettings
        {
            SerializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }
        };

        var startSchema = JsonSchema.FromType<StartNodeConfiguration>(settings);
        var modelSchema = JsonSchema.FromType<ModelNodeConfiguration>(settings);
        var endSchema = JsonSchema.FromType<EndNodeConfiguration>(settings);

        return new List<NodeTypeInfo>
        {
            new NodeTypeInfo
            {
                Type = "start",
                DisplayName = "Start",
                Description = "Entry point - validates input",
                ConfigSchema = JsonSerializer.Deserialize<JsonElement>(startSchema.ToJson())
            },
            new NodeTypeInfo
            {
                Type = "model",
                DisplayName = "Model",
                Description = "Call an LLM",
                ConfigSchema = JsonSerializer.Deserialize<JsonElement>(modelSchema.ToJson())
            },
            new NodeTypeInfo
            {
                Type = "end",
                DisplayName = "End",
                Description = "Output and completion",
                ConfigSchema = JsonSerializer.Deserialize<JsonElement>(endSchema.ToJson())
            }
        };
    }
}
