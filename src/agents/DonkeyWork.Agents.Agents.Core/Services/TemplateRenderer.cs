using System.Text.Json;
using DonkeyWork.Agents.Agents.Contracts.Services;
using Scriban;
using Scriban.Runtime;

namespace DonkeyWork.Agents.Agents.Core.Services;

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

        // Build Scriban context using ScriptObject for proper variable access
        var scriptObject = new ScriptObject();

        // Convert input to navigable object for Scriban
        // Always use a ScriptObject (never null) to allow safe property access
        var inputObject = ConvertToScribanObject(_context.Input) ?? new ScriptObject();
        scriptObject["Input"] = inputObject;

        // Convert node outputs to navigable objects
        var stepsObject = new ScriptObject();
        foreach (var (nodeName, output) in _context.NodeOutputs)
        {
            stepsObject[nodeName] = ConvertToScribanObject(output);
        }
        scriptObject["Steps"] = stepsObject;

        var templateContext = new TemplateContext();
        templateContext.PushGlobal(scriptObject);

        var result = await parsedTemplate.RenderAsync(templateContext);
        return result;
    }

    /// <summary>
    /// Converts an object (potentially JsonElement) to a Scriban-navigable object.
    /// </summary>
    internal static object? ConvertToScribanObject(object? input)
    {
        if (input == null)
            return null;

        if (input is JsonElement element)
        {
            return ConvertJsonElement(element);
        }

        if (input is JsonDocument doc)
        {
            return ConvertJsonElement(doc.RootElement);
        }

        // For other objects, try to convert to ScriptObject if it's a complex type
        var type = input.GetType();
        if (!type.IsPrimitive && type != typeof(string) && !type.IsEnum)
        {
            return ConvertObjectToScriptObject(input);
        }

        return input;
    }

    /// <summary>
    /// Converts a CLR object to a ScriptObject for Scriban navigation.
    /// </summary>
    private static ScriptObject ConvertObjectToScriptObject(object obj)
    {
        var scriptObject = new ScriptObject();
        var type = obj.GetType();

        foreach (var property in type.GetProperties())
        {
            if (!property.CanRead)
                continue;

            var value = property.GetValue(obj);
            var convertedValue = ConvertToScribanObject(value);
            scriptObject[property.Name] = convertedValue;
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
