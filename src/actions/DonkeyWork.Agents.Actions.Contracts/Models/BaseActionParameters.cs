using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DonkeyWork.Agents.Actions.Contracts.Models;

/// <summary>
/// Base class for all action node parameters.
/// Provides validation infrastructure and common properties.
/// </summary>
[JsonDerivedType(typeof(BaseActionParameters))]
public abstract class BaseActionParameters
{
    /// <summary>
    /// Version of the action node schema (for backward compatibility)
    /// </summary>
    public virtual string Version { get; init; } = "1.0";

    /// <summary>
    /// Validates the parameters using DataAnnotations and custom logic
    /// </summary>
    public abstract (bool valid, List<ValidationResult> results) IsValid();

    /// <summary>
    /// Helper method to validate using DataAnnotations attributes
    /// </summary>
    protected (bool valid, List<ValidationResult> results) ValidateDataAnnotations()
    {
        var results = new List<ValidationResult>();
        var properties = GetType().GetProperties();

        foreach (var property in properties)
        {
            // Skip version property
            if (property.Name == nameof(Version))
                continue;

            // Check if property is Resolvable<T>
            if (IsResolvableType(property.PropertyType))
            {
                ValidateResolvableProperty(property, results);
            }
            else
            {
                // Standard validation for non-Resolvable properties
                var value = property.GetValue(this);
                var context = new ValidationContext(this)
                {
                    MemberName = property.Name
                };

                Validator.TryValidateProperty(value, context, results);
            }
        }

        return (results.Count == 0, results);
    }

    /// <summary>
    /// Validates a Resolvable<T> property
    /// </summary>
    private void ValidateResolvableProperty(
        System.Reflection.PropertyInfo property,
        List<ValidationResult> results)
    {
        var value = property.GetValue(this);
        if (value == null) return;

        // Get the RawValue property
        var rawValueProperty = property.PropertyType.GetProperty("RawValue");
        var isExpressionProperty = property.PropertyType.GetProperty("IsExpression");

        if (rawValueProperty == null || isExpressionProperty == null)
            return;

        var rawValue = rawValueProperty.GetValue(value) as string;
        var isExpression = (bool)isExpressionProperty.GetValue(value)!;

        // If it's an expression, skip validation (can't validate at design-time)
        if (isExpression)
            return;

        // For literals, validate against attributes
        var requiredAttr = property.GetCustomAttributes(typeof(RequiredAttribute), true)
            .FirstOrDefault() as RequiredAttribute;

        if (requiredAttr != null && string.IsNullOrEmpty(rawValue))
        {
            results.Add(new ValidationResult(
                requiredAttr.ErrorMessage ?? $"{property.Name} is required",
                new[] { property.Name }));
        }

        // For non-string types, try to parse the literal value
        var innerType = property.PropertyType.GetGenericArguments()[0];
        if (innerType != typeof(string) && !string.IsNullOrEmpty(rawValue))
        {
            try
            {
                if (innerType == typeof(int))
                {
                    if (!int.TryParse(rawValue, out var intValue))
                    {
                        results.Add(new ValidationResult(
                            $"{property.Name} must be a valid integer",
                            new[] { property.Name }));
                        return;
                    }

                    // Check Range attribute
                    var rangeAttr = property.GetCustomAttributes(typeof(RangeAttribute), true)
                        .FirstOrDefault() as RangeAttribute;

                    if (rangeAttr != null)
                    {
                        var context = new ValidationContext(this) { MemberName = property.Name };
                        var rangeResult = rangeAttr.GetValidationResult(intValue, context);
                        if (rangeResult != null)
                        {
                            results.Add(rangeResult);
                        }
                    }
                }
                else if (innerType == typeof(double))
                {
                    if (!double.TryParse(rawValue, out var doubleValue))
                    {
                        results.Add(new ValidationResult(
                            $"{property.Name} must be a valid number",
                            new[] { property.Name }));
                        return;
                    }

                    // Check Range attribute
                    var rangeAttr = property.GetCustomAttributes(typeof(RangeAttribute), true)
                        .FirstOrDefault() as RangeAttribute;

                    if (rangeAttr != null)
                    {
                        var context = new ValidationContext(this) { MemberName = property.Name };
                        var rangeResult = rangeAttr.GetValidationResult(doubleValue, context);
                        if (rangeResult != null)
                        {
                            results.Add(rangeResult);
                        }
                    }
                }
                else if (innerType == typeof(bool))
                {
                    if (!bool.TryParse(rawValue, out _))
                    {
                        results.Add(new ValidationResult(
                            $"{property.Name} must be 'true' or 'false'",
                            new[] { property.Name }));
                    }
                }
            }
            catch
            {
                results.Add(new ValidationResult(
                    $"{property.Name} has an invalid value",
                    new[] { property.Name }));
            }
        }
    }

    /// <summary>
    /// Checks if a type is Resolvable&lt;T&gt;
    /// </summary>
    private static bool IsResolvableType(Type type)
    {
        return type.IsGenericType &&
               type.GetGenericTypeDefinition() == typeof(Types.Resolvable<>);
    }
}
