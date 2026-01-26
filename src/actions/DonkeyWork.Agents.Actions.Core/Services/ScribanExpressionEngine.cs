using System.Globalization;
using DonkeyWork.Agents.Actions.Contracts.Services;
using Scriban;
using Scriban.Runtime;

namespace DonkeyWork.Agents.Actions.Core.Services;

/// <summary>
/// Expression engine using Scriban for template evaluation
/// </summary>
public class ScribanExpressionEngine : IExpressionEngine
{
    public string Evaluate(string template, object context)
    {
        try
        {
            var scriptTemplate = Template.Parse(template);
            var scriptContext = CreateScriptContext(context);
            var result = scriptTemplate.Render(scriptContext);
            return result;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to evaluate expression: {template}", ex);
        }
    }

    public T Evaluate<T>(string template, object context)
    {
        var result = Evaluate(template, context);
        return ConvertTo<T>(result);
    }

    private static ScriptObject CreateScriptContext(object context)
    {
        var scriptObject = new ScriptObject();

        if (context != null)
        {
            // Import all properties from context object
            scriptObject.Import(context, renamer: member => member.Name);
        }

        return scriptObject;
    }

    private static T ConvertTo<T>(string value)
    {
        var targetType = typeof(T);

        // Handle nullable types
        var underlyingType = Nullable.GetUnderlyingType(targetType);
        if (underlyingType != null)
        {
            if (string.IsNullOrEmpty(value))
                return default!;
            targetType = underlyingType;
        }

        try
        {
            if (targetType == typeof(string))
                return (T)(object)value;

            if (targetType == typeof(int))
                return (T)(object)int.Parse(value, CultureInfo.InvariantCulture);

            if (targetType == typeof(long))
                return (T)(object)long.Parse(value, CultureInfo.InvariantCulture);

            if (targetType == typeof(double))
                return (T)(object)double.Parse(value, CultureInfo.InvariantCulture);

            if (targetType == typeof(decimal))
                return (T)(object)decimal.Parse(value, CultureInfo.InvariantCulture);

            if (targetType == typeof(bool))
                return (T)(object)bool.Parse(value);

            if (targetType.IsEnum)
                return (T)Enum.Parse(targetType, value, ignoreCase: true);

            // Try Convert.ChangeType as fallback
            return (T)Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
        }
        catch (Exception ex)
        {
            throw new InvalidCastException($"Cannot convert '{value}' to type {typeof(T).Name}", ex);
        }
    }
}
