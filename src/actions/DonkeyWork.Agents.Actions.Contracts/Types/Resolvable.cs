using System.Text.Json;
using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Actions.Contracts.Types;

/// <summary>
/// A generic type that can hold either a literal value or an expression to be resolved at runtime.
/// </summary>
/// <typeparam name="T">The target type after resolution</typeparam>
[JsonConverter(typeof(ResolvableJsonConverterFactory))]
public readonly struct Resolvable<T>
{
    private readonly string? _rawValue;

    /// <summary>
    /// Creates a Resolvable from a string value (could be literal or expression)
    /// </summary>
    public Resolvable(string value)
    {
        _rawValue = value;
    }

    /// <summary>
    /// Creates a Resolvable from a typed value
    /// </summary>
    public Resolvable(T value)
    {
        _rawValue = ConvertToString(value);
    }

    /// <summary>
    /// The raw string value (either literal or expression)
    /// </summary>
    public string RawValue => _rawValue ?? string.Empty;

    /// <summary>
    /// Checks if the value contains expression syntax {{...}}
    /// </summary>
    public bool IsExpression =>
        !string.IsNullOrEmpty(_rawValue) &&
        _rawValue.Contains("{{") &&
        _rawValue.Contains("}}");

    /// <summary>
    /// Checks if the value is EXACTLY {{expression}} with nothing else
    /// </summary>
    public bool IsPureExpression
    {
        get
        {
            if (!IsExpression) return false;
            var trimmed = _rawValue!.Trim();
            return trimmed.StartsWith("{{") &&
                   trimmed.EndsWith("}}") &&
                   trimmed.IndexOf("{{", 2) == -1; // Only one {{ pair
        }
    }

    /// <summary>
    /// Implicit conversion from T to Resolvable&lt;T&gt;
    /// </summary>
    public static implicit operator Resolvable<T>(T value) => new(value);

    /// <summary>
    /// Implicit conversion from string to Resolvable&lt;T&gt;
    /// </summary>
    public static implicit operator Resolvable<T>(string value) => new(value);

    private static string ConvertToString(T? value)
    {
        if (value == null) return string.Empty;

        return value switch
        {
            string s => s,
            bool b => b.ToString().ToLowerInvariant(),
            _ => value.ToString() ?? string.Empty
        };
    }

    public override string ToString() => RawValue;
}

/// <summary>
/// JSON converter factory for Resolvable&lt;T&gt;
/// </summary>
public class ResolvableJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert.IsGenericType &&
               typeToConvert.GetGenericTypeDefinition() == typeof(Resolvable<>);
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var innerType = typeToConvert.GetGenericArguments()[0];
        var converterType = typeof(ResolvableJsonConverter<>).MakeGenericType(innerType);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }
}

/// <summary>
/// JSON converter for Resolvable&lt;T&gt; - serializes/deserializes as string
/// </summary>
public class ResolvableJsonConverter<T> : JsonConverter<Resolvable<T>>
{
    public override Resolvable<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Always read as string
        var value = reader.GetString();
        return new Resolvable<T>(value ?? string.Empty);
    }

    public override void Write(Utf8JsonWriter writer, Resolvable<T> value, JsonSerializerOptions options)
    {
        // Always write as string
        writer.WriteStringValue(value.RawValue);
    }
}
