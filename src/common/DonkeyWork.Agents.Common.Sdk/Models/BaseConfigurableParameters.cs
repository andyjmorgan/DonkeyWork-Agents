namespace DonkeyWork.Agents.Common.Sdk.Models;

/// <summary>
/// Base class for all configurable parameter sets.
/// Provides common functionality for schema generation and parameter resolution.
/// </summary>
public abstract class BaseConfigurableParameters
{
    /// <summary>
    /// Converts this parameter set to a dictionary for serialization.
    /// </summary>
    public virtual IDictionary<string, object?> ToDictionary()
    {
        var result = new Dictionary<string, object?>();
        var properties = GetType().GetProperties();

        foreach (var property in properties)
        {
            var value = property.GetValue(this);
            if (value != null)
            {
                result[ToCamelCase(property.Name)] = value;
            }
        }

        return result;
    }

    /// <summary>
    /// Creates an instance from a dictionary of values.
    /// </summary>
    public static T FromDictionary<T>(IDictionary<string, object?> values) where T : BaseConfigurableParameters, new()
    {
        var instance = new T();
        var properties = typeof(T).GetProperties();

        foreach (var property in properties)
        {
            var key = ToCamelCase(property.Name);
            if (values.TryGetValue(key, out var value) && value != null)
            {
                // Handle Resolvable<T> types
                var propertyType = property.PropertyType;
                if (IsResolvableType(propertyType) && value is string stringValue)
                {
                    var resolvableInstance = CreateResolvableInstance(propertyType, stringValue);
                    property.SetValue(instance, resolvableInstance);
                }
                else
                {
                    property.SetValue(instance, value);
                }
            }
        }

        return instance;
    }

    private static bool IsResolvableType(Type type)
    {
        if (!type.IsGenericType) return false;

        var genericDef = type.GetGenericTypeDefinition();
        return genericDef.Name.StartsWith("Resolvable") ||
               (Nullable.GetUnderlyingType(type) is { } underlying && IsResolvableType(underlying));
    }

    private static object? CreateResolvableInstance(Type resolvableType, string value)
    {
        var actualType = Nullable.GetUnderlyingType(resolvableType) ?? resolvableType;
        return Activator.CreateInstance(actualType, value);
    }

    private static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        return char.ToLowerInvariant(name[0]) + name[1..];
    }
}
