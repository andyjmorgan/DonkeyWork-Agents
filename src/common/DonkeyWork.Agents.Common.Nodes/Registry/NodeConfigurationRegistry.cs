using System.Text.Json;
using DonkeyWork.Agents.Common.Nodes.Configurations;
using DonkeyWork.Agents.Common.Nodes.Enums;

namespace DonkeyWork.Agents.Common.Nodes.Registry;

/// <summary>
/// Registry for node configuration types. Provides serialization/deserialization support.
/// </summary>
public sealed class NodeConfigurationRegistry
{
    private static readonly Lazy<NodeConfigurationRegistry> _instance = new(() => new NodeConfigurationRegistry());

    /// <summary>
    /// Gets the singleton instance of the registry.
    /// </summary>
    public static NodeConfigurationRegistry Instance => _instance.Value;

    private readonly Dictionary<NodeType, Type> _configTypes = new();
    private readonly JsonSerializerOptions _jsonOptions;

    private NodeConfigurationRegistry()
    {
        DiscoverConfigurationTypes();
        _jsonOptions = BuildJsonOptions();
    }

    /// <summary>
    /// Gets the JSON serializer options configured for polymorphic node configuration serialization.
    /// </summary>
    public JsonSerializerOptions JsonOptions => _jsonOptions;

    /// <summary>
    /// Gets all registered configuration types.
    /// </summary>
    public IReadOnlyDictionary<NodeType, Type> ConfigurationTypes => _configTypes;

    /// <summary>
    /// Gets the configuration type for a node type.
    /// </summary>
    public Type GetConfigurationType(NodeType nodeType)
    {
        if (!_configTypes.TryGetValue(nodeType, out var type))
        {
            throw new ArgumentException($"No configuration type registered for node type: {nodeType}", nameof(nodeType));
        }
        return type;
    }

    /// <summary>
    /// Deserializes a JSON string to a NodeConfiguration.
    /// </summary>
    public NodeConfiguration Deserialize(string json)
    {
        return JsonSerializer.Deserialize<NodeConfiguration>(json, _jsonOptions)
            ?? throw new JsonException("Failed to deserialize node configuration");
    }

    /// <summary>
    /// Deserializes a JsonElement to a NodeConfiguration.
    /// </summary>
    public NodeConfiguration Deserialize(JsonElement element)
    {
        return element.Deserialize<NodeConfiguration>(_jsonOptions)
            ?? throw new JsonException("Failed to deserialize node configuration");
    }

    /// <summary>
    /// Serializes a NodeConfiguration to a JSON string.
    /// </summary>
    public string Serialize(NodeConfiguration config)
    {
        return JsonSerializer.Serialize(config, _jsonOptions);
    }

    /// <summary>
    /// Serializes a NodeConfiguration to a JsonElement.
    /// </summary>
    public JsonElement SerializeToElement(NodeConfiguration config)
    {
        var json = JsonSerializer.Serialize(config, _jsonOptions);
        return JsonDocument.Parse(json).RootElement;
    }

    /// <summary>
    /// Deserializes a dictionary of node configurations from JSON.
    /// </summary>
    public Dictionary<string, NodeConfiguration> DeserializeConfigurations(string json)
    {
        return JsonSerializer.Deserialize<Dictionary<string, NodeConfiguration>>(json, _jsonOptions)
            ?? new Dictionary<string, NodeConfiguration>();
    }

    /// <summary>
    /// Serializes a dictionary of node configurations to JSON.
    /// </summary>
    public string SerializeConfigurations(IReadOnlyDictionary<string, NodeConfiguration> configurations)
    {
        return JsonSerializer.Serialize(configurations, _jsonOptions);
    }

    private void DiscoverConfigurationTypes()
    {
        // Register known types
        _configTypes[NodeType.Start] = typeof(StartNodeConfiguration);
        _configTypes[NodeType.End] = typeof(EndNodeConfiguration);
        _configTypes[NodeType.Model] = typeof(ModelNodeConfiguration);
        _configTypes[NodeType.MessageFormatter] = typeof(MessageFormatterNodeConfiguration);
        _configTypes[NodeType.HttpRequest] = typeof(HttpRequestNodeConfiguration);
        _configTypes[NodeType.Sleep] = typeof(SleepNodeConfiguration);
    }

    private static JsonSerializerOptions BuildJsonOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        return options;
    }
}
