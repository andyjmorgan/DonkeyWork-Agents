using System.Text.Json;
using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Actions.Contracts.Types;

/// <summary>
/// A single key-value item where both key and value support variable expressions.
/// </summary>
public class KeyValueItem
{
    /// <summary>
    /// The key (supports variable expressions like {{variableName}})
    /// </summary>
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// The value (supports variable expressions like {{variableName}})
    /// </summary>
    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
}

/// <summary>
/// A collection of key-value pairs that can be either:
/// - A variable expression (e.g., "{{headers}}") that resolves to a dictionary
/// - A list of manual key-value items
/// </summary>
[JsonConverter(typeof(KeyValueCollectionJsonConverter))]
public class KeyValueCollection
{
    /// <summary>
    /// If true, use the Variable property as a reference to resolve.
    /// If false, use the Items list.
    /// </summary>
    [JsonPropertyName("useVariable")]
    public bool UseVariable { get; set; }

    /// <summary>
    /// Variable expression (e.g., "{{headers}}") - used when UseVariable is true
    /// </summary>
    [JsonPropertyName("variable")]
    public string? Variable { get; set; }

    /// <summary>
    /// Manual key-value items - used when UseVariable is false
    /// </summary>
    [JsonPropertyName("items")]
    public List<KeyValueItem> Items { get; set; } = new();

    /// <summary>
    /// Checks if the collection is empty (no variable and no items)
    /// </summary>
    [JsonIgnore]
    public bool IsEmpty => !UseVariable && Items.Count == 0 && string.IsNullOrEmpty(Variable);

    /// <summary>
    /// Creates an empty collection
    /// </summary>
    public KeyValueCollection()
    {
    }

    /// <summary>
    /// Creates a collection from a variable expression
    /// </summary>
    public static KeyValueCollection FromVariable(string variable)
    {
        return new KeyValueCollection
        {
            UseVariable = true,
            Variable = variable
        };
    }

    /// <summary>
    /// Creates a collection from manual items
    /// </summary>
    public static KeyValueCollection FromItems(IEnumerable<KeyValueItem> items)
    {
        return new KeyValueCollection
        {
            UseVariable = false,
            Items = items.ToList()
        };
    }

    /// <summary>
    /// Creates a collection from a dictionary
    /// </summary>
    public static KeyValueCollection FromDictionary(IDictionary<string, string> dict)
    {
        return new KeyValueCollection
        {
            UseVariable = false,
            Items = dict.Select(kv => new KeyValueItem { Key = kv.Key, Value = kv.Value }).ToList()
        };
    }
}

/// <summary>
/// JSON converter for KeyValueCollection that handles both object format and legacy string format
/// </summary>
public class KeyValueCollectionJsonConverter : JsonConverter<KeyValueCollection?>
{
    public override KeyValueCollection? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        // Handle legacy string format (e.g., "Key: Value\nKey2: Value2" or "{{variable}}")
        if (reader.TokenType == JsonTokenType.String)
        {
            var stringValue = reader.GetString();
            if (string.IsNullOrEmpty(stringValue))
            {
                return new KeyValueCollection();
            }

            // Check if it's a pure variable expression
            var trimmed = stringValue.Trim();
            if (trimmed.StartsWith("{{") && trimmed.EndsWith("}}"))
            {
                return KeyValueCollection.FromVariable(trimmed);
            }

            // Parse legacy "Key: Value\n" format
            var items = new List<KeyValueItem>();
            foreach (var line in stringValue.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = line.Split(':', 2);
                if (parts.Length == 2)
                {
                    items.Add(new KeyValueItem
                    {
                        Key = parts[0].Trim(),
                        Value = parts[1].Trim()
                    });
                }
            }
            return KeyValueCollection.FromItems(items);
        }

        // Handle object format
        if (reader.TokenType == JsonTokenType.StartObject)
        {
            var collection = new KeyValueCollection();

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    break;

                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    var propertyName = reader.GetString();
                    reader.Read();

                    switch (propertyName)
                    {
                        case "useVariable":
                            collection.UseVariable = reader.GetBoolean();
                            break;
                        case "variable":
                            collection.Variable = reader.GetString();
                            break;
                        case "items":
                            if (reader.TokenType == JsonTokenType.StartArray)
                            {
                                collection.Items = JsonSerializer.Deserialize<List<KeyValueItem>>(ref reader, options) ?? new();
                            }
                            break;
                    }
                }
            }

            return collection;
        }

        return new KeyValueCollection();
    }

    public override void Write(Utf8JsonWriter writer, KeyValueCollection? value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStartObject();
        writer.WriteBoolean("useVariable", value.UseVariable);
        writer.WriteString("variable", value.Variable);
        writer.WritePropertyName("items");
        JsonSerializer.Serialize(writer, value.Items, options);
        writer.WriteEndObject();
    }
}
