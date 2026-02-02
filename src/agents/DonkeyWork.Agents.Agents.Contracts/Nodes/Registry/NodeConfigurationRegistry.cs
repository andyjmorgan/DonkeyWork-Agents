using System.Text.Json;
using DonkeyWork.Agents.Agents.Contracts.Nodes.Configurations;
using DonkeyWork.Agents.Agents.Contracts.Nodes.Enums;

namespace DonkeyWork.Agents.Agents.Contracts.Nodes.Registry;

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
    public Dictionary<Guid, NodeConfiguration> DeserializeConfigurations(string json)
    {
        return JsonSerializer.Deserialize<Dictionary<Guid, NodeConfiguration>>(json, _jsonOptions)
            ?? new Dictionary<Guid, NodeConfiguration>();
    }

    /// <summary>
    /// Serializes a dictionary of node configurations to JSON.
    /// </summary>
    public string SerializeConfigurations(IReadOnlyDictionary<Guid, NodeConfiguration> configurations)
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
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            // Support Guid dictionary keys
            Converters = { new GuidDictionaryConverterFactory() }
        };

        return options;
    }
}

/// <summary>
/// Factory for creating Guid-keyed dictionary converters.
/// System.Text.Json only supports string keys by default.
/// </summary>
public class GuidDictionaryConverterFactory : System.Text.Json.Serialization.JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        if (!typeToConvert.IsGenericType)
            return false;

        var generic = typeToConvert.GetGenericTypeDefinition();
        if (generic != typeof(Dictionary<,>))
            return false;

        return typeToConvert.GetGenericArguments()[0] == typeof(Guid);
    }

    public override System.Text.Json.Serialization.JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var valueType = typeToConvert.GetGenericArguments()[1];
        var converterType = typeof(GuidDictionaryConverter<>).MakeGenericType(valueType);
        return (System.Text.Json.Serialization.JsonConverter)Activator.CreateInstance(converterType, options)!;
    }
}

/// <summary>
/// Converter for Dictionary&lt;Guid, TValue&gt;.
/// Handles polymorphic deserialization by reordering the type discriminator to be first.
/// </summary>
public class GuidDictionaryConverter<TValue> : System.Text.Json.Serialization.JsonConverter<Dictionary<Guid, TValue>>
{
    private readonly JsonSerializerOptions _valueOptions;
    private const string TypeDiscriminatorProperty = "type";

    public GuidDictionaryConverter(JsonSerializerOptions options)
    {
        // Create options without this converter to avoid infinite recursion
        _valueOptions = new JsonSerializerOptions(options);
        var thisFactory = _valueOptions.Converters.FirstOrDefault(c => c is GuidDictionaryConverterFactory);
        if (thisFactory != null)
            _valueOptions.Converters.Remove(thisFactory);
    }

    public override Dictionary<Guid, TValue>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected StartObject");

        var result = new Dictionary<Guid, TValue>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                return result;

            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException("Expected PropertyName");

            var keyString = reader.GetString()!;
            if (!Guid.TryParse(keyString, out var key))
                throw new JsonException($"Cannot parse '{keyString}' as Guid");

            reader.Read();

            // For polymorphic types, we need to ensure the type discriminator is first
            // Read the entire value as a JsonDocument, reorder if needed, then deserialize
            if (typeof(TValue).IsAbstract || typeof(TValue).IsInterface)
            {
                using var doc = JsonDocument.ParseValue(ref reader);
                var reorderedJson = ReorderTypeDiscriminator(doc.RootElement);
                var value = JsonSerializer.Deserialize<TValue>(reorderedJson, _valueOptions);
                result[key] = value!;
            }
            else
            {
                var value = JsonSerializer.Deserialize<TValue>(ref reader, _valueOptions);
                result[key] = value!;
            }
        }

        throw new JsonException("Expected EndObject");
    }

    /// <summary>
    /// Reorders JSON object to ensure "type" discriminator is the first property.
    /// System.Text.Json polymorphic deserialization requires the discriminator first.
    /// </summary>
    private static string ReorderTypeDiscriminator(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return element.GetRawText();

        var properties = new List<(string Name, JsonElement Value)>();
        string? typeValue = null;

        foreach (var prop in element.EnumerateObject())
        {
            if (prop.Name == TypeDiscriminatorProperty)
            {
                typeValue = prop.Value.GetRawText();
            }
            else
            {
                properties.Add((prop.Name, prop.Value));
            }
        }

        // If no type discriminator found, return original
        if (typeValue == null)
            return element.GetRawText();

        // Build reordered JSON with type first
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        writer.WriteStartObject();
        writer.WritePropertyName(TypeDiscriminatorProperty);
        writer.WriteRawValue(typeValue);

        foreach (var (name, value) in properties)
        {
            writer.WritePropertyName(name);
            value.WriteTo(writer);
        }

        writer.WriteEndObject();
        writer.Flush();

        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    public override void Write(Utf8JsonWriter writer, Dictionary<Guid, TValue> value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        foreach (var kvp in value)
        {
            writer.WritePropertyName(kvp.Key.ToString());

            // For polymorphic types, ensure type discriminator is written first
            if (typeof(TValue).IsAbstract || typeof(TValue).IsInterface)
            {
                // Serialize to intermediate JSON, then reorder
                var json = JsonSerializer.Serialize(kvp.Value, _valueOptions);
                using var doc = JsonDocument.Parse(json);
                var reordered = ReorderTypeDiscriminator(doc.RootElement);
                writer.WriteRawValue(reordered);
            }
            else
            {
                JsonSerializer.Serialize(writer, kvp.Value, _valueOptions);
            }
        }

        writer.WriteEndObject();
    }
}
