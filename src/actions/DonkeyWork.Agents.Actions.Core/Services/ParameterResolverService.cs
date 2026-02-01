using System.Globalization;
using DonkeyWork.Agents.Actions.Contracts.Services;
using DonkeyWork.Agents.Common.Sdk.Types;

namespace DonkeyWork.Agents.Actions.Core.Services;

/// <summary>
/// Service for resolving Resolvable&lt;T&gt; parameters
/// </summary>
public class ParameterResolverService : IParameterResolver
{
    private readonly IExpressionEngine _expressionEngine;

    public ParameterResolverService(IExpressionEngine expressionEngine)
    {
        _expressionEngine = expressionEngine;
    }

    public T Resolve<T>(Resolvable<T> resolvable, object? context = null)
    {
        var rawValue = resolvable.RawValue;

        // If it's an expression, evaluate it
        if (resolvable.IsExpression)
        {
            if (context == null)
            {
                throw new InvalidOperationException(
                    $"Cannot resolve expression '{rawValue}' without context");
            }

            return _expressionEngine.Evaluate<T>(rawValue, context);
        }

        // It's a literal value, parse it
        return ParseLiteral<T>(rawValue);
    }

    public string ResolveString(string value, object? context = null)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        // Check if it contains expressions
        if (value.Contains("{{") && value.Contains("}}"))
        {
            if (context == null)
            {
                throw new InvalidOperationException(
                    $"Cannot resolve expression in '{value}' without context");
            }

            return _expressionEngine.Evaluate(value, context);
        }

        return value;
    }

    public Dictionary<string, string> ResolveHeaders(string variable, object? context = null)
    {
        if (string.IsNullOrEmpty(variable))
            return new Dictionary<string, string>();

        if (context == null)
        {
            throw new InvalidOperationException(
                $"Cannot resolve headers expression '{variable}' without context");
        }

        // Evaluate the expression to get a dictionary
        var result = _expressionEngine.Evaluate<Dictionary<string, string>>(variable, context);
        return result ?? new Dictionary<string, string>();
    }

    private static T ParseLiteral<T>(string value)
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
            throw new FormatException($"Cannot parse '{value}' as {typeof(T).Name}", ex);
        }
    }
}
