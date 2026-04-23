using System.Text.Json;
using DonkeyWork.Agents.Orchestrations.Contracts.Services;
using Scriban;
using Scriban.Runtime;

namespace DonkeyWork.Agents.Orchestrations.Core.Services;

/// <summary>
/// Scriban template renderer that uses the execution context for variables.
/// </summary>
public class TemplateRenderer : ITemplateRenderer
{
    private readonly IExecutionContext _context;

    public TemplateRenderer(IExecutionContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<string> RenderAsync(string template, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(template))
            return string.Empty;

        var parsedTemplate = Template.Parse(template);

        if (parsedTemplate.HasErrors)
        {
            var errors = string.Join("; ", parsedTemplate.Messages.Select(m => m.Message));
            throw new InvalidOperationException($"Template parse error: {errors}");
        }

        var scriptObject = new ScriptObject();

        // Always use a ScriptObject (never null) to allow safe property access
        var inputObject = ConvertToScribanObject(_context.Input) ?? new ScriptObject();
        scriptObject["Input"] = inputObject;

        var stepsObject = new ScriptObject();
        foreach (var (nodeName, output) in _context.NodeOutputs)
        {
            stepsObject[nodeName] = ConvertToScribanObject(output);
        }
        scriptObject["Steps"] = stepsObject;

        // Register custom functions
        var customFunctions = new ScriptObject();
        customFunctions.Import("json_value", new Func<string, string, string>(JsonValueFunction));
        customFunctions.Import("to_json", new Func<object?, string>(ToJsonFunction));
        scriptObject.Import(customFunctions);

        var templateContext = new TemplateContext();
        templateContext.PushGlobal(scriptObject);

        var result = await parsedTemplate.RenderAsync(templateContext);
        return result;
    }

    /// <summary>
    /// Extracts a property value from a JSON string.
    /// Usage in Scriban: {{ Steps.model.ResponseText | json_value "name" }}
    /// </summary>
    private static string JsonValueFunction(string json, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(json) || string.IsNullOrWhiteSpace(propertyName))
            return string.Empty;

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty(propertyName, out var value))
            {
                return value.ValueKind == JsonValueKind.String
                    ? value.GetString() ?? string.Empty
                    : value.GetRawText();
            }

            return string.Empty;
        }
        catch (JsonException)
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Serializes a value to JSON. Lets callers pipe an upstream array into a downstream
    /// node's input (e.g. ChunkText.Chunks → TextToSpeech.Inputs).
    /// Usage in Scriban: {{ Steps.chunker.Chunks | to_json }}
    /// </summary>
    private static string ToJsonFunction(object? value)
    {
        if (value is null)
        {
            return "null";
        }

        var normalized = UnwrapForJson(value);
        return JsonSerializer.Serialize(normalized);
    }

    private static object? UnwrapForJson(object? value)
    {
        if (value is null)
        {
            return null;
        }

        if (value is ScriptObject so)
        {
            var dict = new Dictionary<string, object?>();
            foreach (var key in so.Keys)
            {
                dict[key] = UnwrapForJson(so[key]);
            }
            return dict;
        }

        if (value is string)
        {
            return value;
        }

        if (value is System.Collections.IEnumerable enumerable)
        {
            var list = new List<object?>();
            foreach (var item in enumerable)
            {
                list.Add(UnwrapForJson(item));
            }
            return list;
        }

        return value;
    }

    /// <summary>
    /// Converts an object (potentially JsonElement) to a Scriban-navigable object.
    /// </summary>
    private const int MaxDepth = 10;

    internal static object? ConvertToScribanObject(object? input)
    {
        return ConvertToScribanObject(input, 0);
    }

    private static object? ConvertToScribanObject(object? input, int depth)
    {
        if (input == null || depth > MaxDepth)
            return null;

        if (input is JsonElement element)
            return ConvertJsonElement(element);

        if (input is JsonDocument doc)
            return ConvertJsonElement(doc.RootElement);

        if (input is string s)
            return s;

        if (input is ScriptObject)
            return input;

        if (input is IDictionary<string, object?> dict)
        {
            var so = new ScriptObject();
            foreach (var (key, value) in dict)
                so[key] = ConvertToScribanObject(value, depth + 1);
            return so;
        }

        if (input is System.Collections.IEnumerable enumerable and not string)
        {
            var list = new List<object?>();
            foreach (var item in enumerable)
                list.Add(ConvertToScribanObject(item, depth + 1));
            return list;
        }

        var type = input.GetType();
        if (!type.IsPrimitive && !type.IsEnum && type != typeof(decimal) && type != typeof(DateTime) && type != typeof(DateTimeOffset))
            return ConvertObjectToScriptObject(input, depth);

        return input;
    }

    private static ScriptObject ConvertObjectToScriptObject(object obj, int depth)
    {
        var scriptObject = new ScriptObject();
        var type = obj.GetType();

        foreach (var property in type.GetProperties())
        {
            if (!property.CanRead || property.GetIndexParameters().Length > 0)
                continue;

            try
            {
                var value = property.GetValue(obj);
                scriptObject[property.Name] = ConvertToScribanObject(value, depth + 1);
            }
            catch
            {
                // Skip properties that throw on access
            }
        }

        return scriptObject;
    }

    /// <summary>
    /// Recursively converts a JsonElement to a ScriptObject/list structure that Scriban can navigate.
    /// </summary>
    private static object? ConvertJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => ConvertJsonObjectToScriptObject(element),
            JsonValueKind.Array => element.EnumerateArray()
                .Select(ConvertJsonElement)
                .ToList(),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => element.GetRawText()
        };
    }

    /// <summary>
    /// Converts a JSON object to a ScriptObject for proper Scriban navigation.
    /// </summary>
    private static ScriptObject ConvertJsonObjectToScriptObject(JsonElement element)
    {
        var scriptObject = new ScriptObject();
        foreach (var prop in element.EnumerateObject())
        {
            scriptObject[prop.Name] = ConvertJsonElement(prop.Value);
        }
        return scriptObject;
    }
}
