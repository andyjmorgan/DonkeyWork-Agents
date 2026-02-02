using System.Reflection;
using DonkeyWork.Agents.Orchestrations.Contracts.Models;
using DonkeyWork.Agents.Orchestrations.Contracts.Services;
using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Attributes;
using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Configurations;
using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Schema;

namespace DonkeyWork.Agents.Orchestrations.Core.Services;

/// <summary>
/// Provides node type information and configuration schemas.
/// Reads all metadata from NodeAttribute on configuration classes.
/// </summary>
public class NodeTypeSchemaService : INodeTypeSchemaService
{
    private readonly INodeSchemaGenerator _schemaGenerator;
    private readonly Lazy<IReadOnlyList<NodeTypeInfo>> _nodeTypes;

    public NodeTypeSchemaService(INodeSchemaGenerator schemaGenerator)
    {
        _schemaGenerator = schemaGenerator;
        _nodeTypes = new Lazy<IReadOnlyList<NodeTypeInfo>>(GenerateNodeTypes);
    }

    public IReadOnlyList<NodeTypeInfo> GetNodeTypes()
    {
        return _nodeTypes.Value;
    }

    private IReadOnlyList<NodeTypeInfo> GenerateNodeTypes()
    {
        var result = new List<NodeTypeInfo>();
        var assembly = typeof(NodeConfiguration).Assembly;

        // Find all concrete NodeConfiguration classes
        var configTypes = assembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(NodeConfiguration)) && !t.IsAbstract);

        foreach (var configType in configTypes)
        {
            // Read NodeAttribute from the class
            var nodeAttr = configType.GetCustomAttribute<NodeAttribute>();
            if (nodeAttr == null)
            {
                // Skip configurations without NodeAttribute
                continue;
            }

            // Create minimal instance to get NodeType
            var instance = CreateMinimalInstance(configType);
            if (instance == null)
            {
                continue;
            }

            var nodeType = instance.NodeType;

            result.Add(new NodeTypeInfo
            {
                Type = nodeType,
                DisplayName = nodeAttr.DisplayName,
                Description = nodeAttr.Description,
                Category = nodeAttr.Category,
                Icon = nodeAttr.Icon,
                Color = nodeAttr.Color,
                HasInputHandle = nodeAttr.HasInputHandle,
                HasOutputHandle = nodeAttr.HasOutputHandle,
                CanDelete = nodeAttr.CanDelete,
                ConfigSchema = _schemaGenerator.GenerateSchema(nodeType)
            });
        }

        return result;
    }

    private static NodeConfiguration? CreateMinimalInstance(Type configType)
    {
        try
        {
            // Create minimal instance for type discovery (required properties need values)
            return configType.Name switch
            {
                nameof(StartNodeConfiguration) => new StartNodeConfiguration
                {
                    Name = "temp",
                    InputSchema = System.Text.Json.JsonDocument.Parse("{}").RootElement
                },
                nameof(EndNodeConfiguration) => new EndNodeConfiguration { Name = "temp" },
                nameof(ModelNodeConfiguration) => new ModelNodeConfiguration
                {
                    Name = "temp",
                    Provider = Common.Contracts.Enums.LlmProvider.OpenAI,
                    ModelId = "temp",
                    CredentialId = Guid.Empty,
                    UserMessages = []
                },
                nameof(MessageFormatterNodeConfiguration) => new MessageFormatterNodeConfiguration
                {
                    Name = "temp",
                    Template = ""
                },
                nameof(HttpRequestNodeConfiguration) => new HttpRequestNodeConfiguration
                {
                    Name = "temp",
                    Method = Contracts.Nodes.Enums.HttpMethod.Get,
                    Url = ""
                },
                nameof(SleepNodeConfiguration) => new SleepNodeConfiguration
                {
                    Name = "temp",
                    DurationSeconds = 0
                },
                _ => null
            };
        }
        catch
        {
            return null;
        }
    }
}
