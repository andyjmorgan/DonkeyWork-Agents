using System.Reflection;
using System.Text.Json;
using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Attributes;
using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Configurations;
using DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Enums;

namespace DonkeyWork.Agents.Orchestrations.Contracts.Nodes.Schema;

/// <summary>
/// Generates configuration schemas from node configuration types using reflection on attributes.
/// </summary>
public sealed class NodeSchemaGenerator : INodeSchemaGenerator
{
    private readonly Dictionary<NodeType, Type> _configTypes;
    private readonly Dictionary<NodeType, NodeConfigSchema> _schemaCache = new();

    public NodeSchemaGenerator()
    {
        _configTypes = DiscoverConfigurationTypes();
    }

    /// <inheritdoc />
    public NodeConfigSchema GenerateSchema(NodeType nodeType)
    {
        if (_schemaCache.TryGetValue(nodeType, out var cached))
        {
            return cached;
        }

        if (!_configTypes.TryGetValue(nodeType, out var configType))
        {
            throw new ArgumentException($"No configuration type found for node type: {nodeType}", nameof(nodeType));
        }

        var schema = GenerateSchemaFromType(configType, nodeType);
        _schemaCache[nodeType] = schema;
        return schema;
    }

    /// <inheritdoc />
    public NodeConfigSchema GenerateSchema<TConfig>() where TConfig : NodeConfiguration
    {
        var configType = typeof(TConfig);
        var instance = CreateInstance(configType);
        if (instance == null)
        {
            throw new InvalidOperationException($"Cannot create instance of {configType.Name}");
        }

        var nodeType = instance.NodeType;

        if (_schemaCache.TryGetValue(nodeType, out var cached))
        {
            return cached;
        }

        var schema = GenerateSchemaFromType(configType, nodeType);
        _schemaCache[nodeType] = schema;
        return schema;
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<NodeType, NodeConfigSchema> GetAllSchemas()
    {
        foreach (var nodeType in _configTypes.Keys)
        {
            if (!_schemaCache.ContainsKey(nodeType))
            {
                GenerateSchema(nodeType);
            }
        }

        return _schemaCache;
    }

    private static Dictionary<NodeType, Type> DiscoverConfigurationTypes()
    {
        var result = new Dictionary<NodeType, Type>();
        var assembly = typeof(NodeConfiguration).Assembly;
        var configTypes = assembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(NodeConfiguration)) && !t.IsAbstract);

        foreach (var type in configTypes)
        {
            var instance = CreateInstance(type);
            if (instance != null)
            {
                result[instance.NodeType] = type;
            }
        }

        return result;
    }

    private static NodeConfiguration? CreateInstance(Type configType)
    {
        try
        {
            // We need to provide required properties
            var name = "temp";
            var constructors = configType.GetConstructors();

            // Try to create using default values
            if (configType == typeof(StartNodeConfiguration))
            {
                return new StartNodeConfiguration { Name = name, InputSchema = JsonDocument.Parse("{}").RootElement };
            }
            if (configType == typeof(EndNodeConfiguration))
            {
                return new EndNodeConfiguration { Name = name };
            }
            if (configType == typeof(ModelNodeConfiguration))
            {
                return new ModelNodeConfiguration
                {
                    Name = name,
                    Provider = Common.Contracts.Enums.LlmProvider.OpenAI,
                    ModelId = "temp",
                    CredentialId = Guid.Empty,
                    UserMessages = []
                };
            }
            if (configType == typeof(MessageFormatterNodeConfiguration))
            {
                return new MessageFormatterNodeConfiguration { Name = name, Template = "" };
            }
            if (configType == typeof(HttpRequestNodeConfiguration))
            {
                return new HttpRequestNodeConfiguration
                {
                    Name = name,
                    Method = Enums.HttpMethod.Get,
                    Url = ""
                };
            }
            if (configType == typeof(SleepNodeConfiguration))
            {
                return new SleepNodeConfiguration { Name = name, DurationSeconds = 0 };
            }
            if (configType == typeof(TextToSpeechNodeConfiguration))
            {
                return new TextToSpeechNodeConfiguration
                {
                    Name = name,
                    CredentialId = Guid.Empty,
                    Model = "tts-1",
                    Voice = "alloy",
                    InputText = ""
                };
            }
            if (configType == typeof(StoreAudioNodeConfiguration))
            {
                return new StoreAudioNodeConfiguration
                {
                    Name = name,
                    RecordingName = "",
                    RecordingDescription = "",
                    AudioObjectKey = "",
                    Transcript = "",
                    Voice = "",
                    Model = ""
                };
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private static NodeConfigSchema GenerateSchemaFromType(Type configType, NodeType nodeType)
    {
        var tabs = new Dictionary<string, TabSchema>();
        var fields = new List<FieldSchema>();

        var properties = configType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            // Skip the NodeType property (it's abstract/computed)
            if (property.Name == "NodeType")
                continue;

            var fieldAttr = property.GetCustomAttribute<ConfigurableFieldAttribute>();
            if (fieldAttr == null)
                continue;

            var tabAttr = property.GetCustomAttribute<TabAttribute>();
            var sliderAttr = property.GetCustomAttribute<SliderAttribute>();
            var selectOptionsAttr = property.GetCustomAttribute<SelectOptionsAttribute>();
            var reliesUponAttr = property.GetCustomAttribute<ReliesUponAttribute>();
            var immutableAttr = property.GetCustomAttribute<ImmutableAttribute>();
            var supportsVariables = property.GetCustomAttribute<SupportVariablesAttribute>() != null;

            if (tabAttr != null && !tabs.ContainsKey(tabAttr.Name))
            {
                tabs[tabAttr.Name] = new TabSchema
                {
                    Name = tabAttr.Name,
                    Order = tabAttr.Order,
                    Icon = tabAttr.Icon
                };
            }

            var options = GetEnumOptions(property.PropertyType) ?? selectOptionsAttr?.Options;
            var defaultValue = sliderAttr?.HasDefault == true
                ? sliderAttr.Default
                : selectOptionsAttr?.Default as object;

            var fieldSchema = new FieldSchema
            {
                Name = ToCamelCase(property.Name),
                Label = fieldAttr.Label,
                Description = fieldAttr.Description,
                PropertyType = GetPropertyTypeName(property.PropertyType),
                ControlType = fieldAttr.ControlType,
                Order = fieldAttr.Order,
                Tab = tabAttr?.Name,
                Required = fieldAttr.Required || IsRequiredProperty(property),
                SupportsVariables = supportsVariables,
                Placeholder = fieldAttr.Placeholder,
                DefaultValue = defaultValue,
                Min = sliderAttr?.Min,
                Max = sliderAttr?.Max,
                Step = sliderAttr?.Step,
                Options = options,
                Immutable = immutableAttr != null,
                ReliesUpon = reliesUponAttr != null
                    ? new ReliesUponSchema
                    {
                        FieldName = ToCamelCase(reliesUponAttr.FieldName),
                        Value = reliesUponAttr.Value,
                        RequiredWhenEnabled = reliesUponAttr.RequiredWhenEnabled
                    }
                    : null
            };

            fields.Add(fieldSchema);
        }

        return new NodeConfigSchema
        {
            NodeType = nodeType,
            Tabs = tabs.Values.OrderBy(t => t.Order).ToList(),
            Fields = fields.OrderBy(f => f.Order).ToList()
        };
    }

    private static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;
        return char.ToLowerInvariant(name[0]) + name[1..];
    }

    private static string GetPropertyTypeName(Type type)
    {
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        if (underlyingType == typeof(string))
            return "string";
        if (underlyingType == typeof(int) || underlyingType == typeof(long) ||
            underlyingType == typeof(double) || underlyingType == typeof(float) ||
            underlyingType == typeof(decimal))
            return "number";
        if (underlyingType == typeof(bool))
            return "boolean";
        if (underlyingType == typeof(Guid))
            return "string"; // GUIDs are serialized as strings
        if (underlyingType == typeof(JsonElement))
            return "object";
        if (underlyingType.IsEnum)
            return "enum";
        if (underlyingType.IsGenericType)
        {
            var genericDef = underlyingType.GetGenericTypeDefinition();
            if (genericDef == typeof(List<>) || genericDef == typeof(IList<>) ||
                genericDef == typeof(IReadOnlyList<>) || genericDef == typeof(IEnumerable<>))
            {
                return "array";
            }
        }
        if (underlyingType == typeof(Types.KeyValueCollection))
            return "keyValueCollection";

        return "object";
    }

    private static bool IsRequiredProperty(PropertyInfo property)
    {
        var nullabilityContext = new NullabilityInfoContext();
        var nullabilityInfo = nullabilityContext.Create(property);
        return nullabilityInfo.WriteState == NullabilityState.NotNull;
    }

    private static IReadOnlyList<string>? GetEnumOptions(Type type)
    {
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;
        if (!underlyingType.IsEnum)
            return null;

        return Enum.GetNames(underlyingType);
    }
}
