using System.Reflection;
using DonkeyWork.Agents.Orchestrations.Contracts.Models;
using DonkeyWork.Agents.Orchestrations.Contracts.Services;
using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Attributes;
using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Configurations;
using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Enums;
using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Providers;
using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Schema;
using DonkeyWork.Agents.Orchestrations.Core.Execution;
using DonkeyWork.Agents.Orchestrations.Core.Execution.Executors;

namespace DonkeyWork.Agents.Orchestrations.Core.Services;

public class NodeTypeSchemaService : INodeTypeSchemaService
{
    private readonly INodeSchemaGenerator _schemaGenerator;
    private readonly NodeMethodRegistry _methodRegistry;
    private readonly Lazy<IReadOnlyList<NodeTypeInfo>> _nodeTypes;

    public NodeTypeSchemaService(INodeSchemaGenerator schemaGenerator, NodeMethodRegistry methodRegistry)
    {
        _schemaGenerator = schemaGenerator;
        _methodRegistry = methodRegistry;
        _nodeTypes = new Lazy<IReadOnlyList<NodeTypeInfo>>(GenerateNodeTypes);
    }

    public IReadOnlyList<NodeTypeInfo> GetNodeTypes()
    {
        return _nodeTypes.Value;
    }

    private static readonly Dictionary<NodeType, Type> DedicatedExecutors = new()
    {
        [NodeType.Start] = typeof(StartNodeExecutor),
        [NodeType.End] = typeof(EndNodeExecutor),
        [NodeType.Model] = typeof(ModelNodeExecutor),
        [NodeType.MultimodalChatModel] = typeof(MultimodalChatNodeExecutor),
        [NodeType.TextToSpeech] = typeof(TextToSpeechNodeExecutor),
        [NodeType.GeminiTextToSpeech] = typeof(GeminiTextToSpeechNodeExecutor),
        [NodeType.StoreAudio] = typeof(StoreAudioNodeExecutor),
        [NodeType.ConcatAudio] = typeof(ConcatAudioNodeExecutor),
    };

    private IReadOnlyList<string>? GetOutputPropertiesForNodeType(NodeType nodeType)
    {
        Type? outputType = null;

        if (DedicatedExecutors.TryGetValue(nodeType, out var executorType))
        {
            var baseType = executorType.BaseType;
            if (baseType is { IsGenericType: true } &&
                baseType.GetGenericTypeDefinition() == typeof(NodeExecutor<,>))
            {
                outputType = baseType.GetGenericArguments()[1];
            }
        }
        else if (_methodRegistry.HasMethod(nodeType))
        {
            outputType = _methodRegistry.GetMethod(nodeType).OutputType;
        }

        if (outputType == null)
            return null;

        var properties = outputType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(p => p.CanRead && p.Name != "ToMessageOutput")
            .Select(p => p.Name)
            .ToList();

        return properties.Count > 0 ? properties : null;
    }

    private IReadOnlyList<NodeTypeInfo> GenerateNodeTypes()
    {
        var result = new List<NodeTypeInfo>();
        var assembly = typeof(NodeConfiguration).Assembly;

        var configTypes = assembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(NodeConfiguration)) && !t.IsAbstract);

        foreach (var configType in configTypes)
        {
            var nodeAttr = configType.GetCustomAttribute<NodeAttribute>();
            if (nodeAttr == null)
                continue;

            var instance = CreateMinimalInstance(configType);
            if (instance == null)
                continue;

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
                ConfigSchema = _schemaGenerator.GenerateSchema(nodeType),
                OutputProperties = GetOutputPropertiesForNodeType(nodeType)
            });
        }

        return result;
    }

    private static NodeConfiguration? CreateMinimalInstance(Type configType)
    {
        try
        {
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
                nameof(TextToSpeechNodeConfiguration) => new TextToSpeechNodeConfiguration
                {
                    Name = "temp",
                    CredentialId = Guid.Empty,
                    Model = "tts-1",
                    Voice = "alloy",
                    Text = ""
                },
                nameof(GeminiTextToSpeechNodeConfiguration) => new GeminiTextToSpeechNodeConfiguration
                {
                    Name = "temp",
                    CredentialId = Guid.Empty,
                    Model = "gemini-2.5-flash-preview-tts",
                    Voice = "Kore",
                    Text = ""
                },
                nameof(StoreAudioNodeConfiguration) => new StoreAudioNodeConfiguration
                {
                    Name = "temp",
                    RecordingName = "",
                    RecordingDescription = "",
                    AudioBase64 = "",
                    ContentType = ""
                },
                nameof(ConcatAudioNodeConfiguration) => new ConcatAudioNodeConfiguration
                {
                    Name = "temp",
                    SourceNode = ""
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
