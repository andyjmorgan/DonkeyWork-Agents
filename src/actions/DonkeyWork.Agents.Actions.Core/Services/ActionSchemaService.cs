using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json;
using DonkeyWork.Agents.Actions.Contracts.Attributes;
using DonkeyWork.Agents.Actions.Contracts.Models.Schema;
using DonkeyWork.Agents.Actions.Contracts.Services;
using DonkeyWork.Agents.Common.Sdk.Types;

namespace DonkeyWork.Agents.Actions.Core.Services;

/// <summary>
/// Service for generating action node schemas from C# types via reflection
/// </summary>
public class ActionSchemaService : IActionSchemaService
{
    public List<ActionNodeSchema> GenerateSchemas(Assembly assembly)
    {
        var schemas = new List<ActionNodeSchema>();

        // Find all types with [ActionNode] attribute
        var actionNodeTypes = assembly.GetTypes()
            .Where(t => t.GetCustomAttribute<ActionNodeAttribute>() != null);

        foreach (var type in actionNodeTypes)
        {
            schemas.Add(GenerateSchema(type));
        }

        return schemas;
    }

    public ActionNodeSchema GenerateSchema(Type parameterType)
    {
        var actionNodeAttr = parameterType.GetCustomAttribute<ActionNodeAttribute>();
        if (actionNodeAttr == null)
        {
            throw new InvalidOperationException(
                $"Type {parameterType.Name} does not have [ActionNode] attribute");
        }

        var schema = new ActionNodeSchema
        {
            ActionType = actionNodeAttr.ActionType,
            DisplayName = actionNodeAttr.DisplayName ?? parameterType.Name,
            Category = actionNodeAttr.Category,
            Group = actionNodeAttr.Group,
            Icon = actionNodeAttr.Icon,
            Description = actionNodeAttr.Description,
            MaxInputs = actionNodeAttr.MaxInputs,
            MaxOutputs = actionNodeAttr.MaxOutputs,
            Enabled = actionNodeAttr.Enabled,
            Parameters = new List<ParameterSchema>()
        };

        // Generate parameter schemas
        var properties = parameterType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var prop in properties)
        {
            // Skip inherited base properties we don't need in schema
            if (prop.Name == "Version")
                continue;

            var paramSchema = GenerateParameterSchema(prop);
            schema.Parameters.Add(paramSchema);
        }

        return schema;
    }

    public string ExportAsJson(List<ActionNodeSchema> schemas)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        return JsonSerializer.Serialize(schemas, options);
    }

    private ParameterSchema GenerateParameterSchema(PropertyInfo property)
    {
        var schema = new ParameterSchema
        {
            Name = property.Name,
            DisplayName = GetDisplayName(property),
            Description = GetDescription(property),
            Type = GetParameterType(property),
            Required = IsRequired(property),
            DefaultValue = GetDefaultValue(property),
            SupportsVariables = HasAttribute<SupportVariablesAttribute>(property),
            EditorType = GetEditorType(property),
            ControlType = GetControlType(property),
            Resolvable = IsResolvableProperty(property),
            CredentialTypes = GetCredentialTypes(property),
            Dependency = GetDependency(property),
            Validation = GetValidation(property),
            Options = GetOptions(property)
        };

        return schema;
    }

    private string GetDisplayName(PropertyInfo property)
    {
        var displayAttr = property.GetCustomAttribute<DisplayAttribute>();
        return displayAttr?.Name ?? property.Name;
    }

    private string? GetDescription(PropertyInfo property)
    {
        var displayAttr = property.GetCustomAttribute<DisplayAttribute>();
        return displayAttr?.Description;
    }

    private string GetParameterType(PropertyInfo property)
    {
        var type = property.PropertyType;

        // Handle nullable types first (e.g., Resolvable<T>? -> Resolvable<T>)
        var underlyingType = Nullable.GetUnderlyingType(type);
        if (underlyingType != null)
        {
            type = underlyingType;
        }

        // Handle Resolvable<T>
        if (IsResolvableType(type))
        {
            var innerType = type.GetGenericArguments()[0];
            return GetSimpleTypeName(innerType);
        }

        return GetSimpleTypeName(type);
    }

    private string GetSimpleTypeName(Type type)
    {
        if (type.IsEnum)
            return "enum";

        if (type == typeof(KeyValueCollection))
            return "keyValueCollection";

        return Type.GetTypeCode(type) switch
        {
            TypeCode.Boolean => "boolean",
            TypeCode.Int32 or TypeCode.Int64 or TypeCode.Double or TypeCode.Decimal => "number",
            TypeCode.String => "string",
            _ => type.Name.ToLowerInvariant()
        };
    }

    private bool IsRequired(PropertyInfo property)
    {
        return property.GetCustomAttribute<RequiredAttribute>() != null;
    }

    private string? GetDefaultValue(PropertyInfo property)
    {
        var defaultAttr = property.GetCustomAttribute<DefaultValueAttribute>();
        return defaultAttr?.Value?.ToString();
    }

    private string? GetEditorType(PropertyInfo property)
    {
        var editorAttr = property.GetCustomAttribute<EditorTypeAttribute>();
        return editorAttr?.EditorType.ToString();
    }

    private string? GetControlType(PropertyInfo property)
    {
        // Determine control type based on attributes and type
        if (HasAttribute<SliderAttribute>(property))
            return "slider";

        var editorAttr = property.GetCustomAttribute<EditorTypeAttribute>();
        if (editorAttr != null)
        {
            return editorAttr.EditorType switch
            {
                EditorType.Text => "text",
                EditorType.TextArea => "textarea",
                EditorType.Code => "code",
                EditorType.Dropdown => "dropdown",
                EditorType.Number => "number",
                EditorType.Checkbox => "checkbox",
                EditorType.Slider => "slider",
                EditorType.DateTime => "datetime",
                EditorType.Json => "json",
                EditorType.KeyValueList => "keyValueList",
                _ => null
            };
        }

        // Auto-detect from type
        var type = property.PropertyType;

        // Handle nullable types first
        var underlyingType = Nullable.GetUnderlyingType(type);
        if (underlyingType != null)
            type = underlyingType;

        if (IsResolvableType(type))
            type = type.GetGenericArguments()[0];

        if (type == typeof(bool))
            return "checkbox";
        if (type.IsEnum)
            return "dropdown";
        if (IsNumericType(type))
            return "number";

        return "text";
    }

    private bool IsResolvableType(Type type)
    {
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Resolvable<>);
    }

    private bool IsResolvableProperty(PropertyInfo property)
    {
        var type = property.PropertyType;

        // Handle nullable types (e.g., Resolvable<T>?)
        var underlyingType = Nullable.GetUnderlyingType(type);
        if (underlyingType != null)
        {
            type = underlyingType;
        }

        return IsResolvableType(type);
    }

    private bool IsNumericType(Type type)
    {
        return Type.GetTypeCode(type) switch
        {
            TypeCode.Int32 or TypeCode.Int64 or TypeCode.Double or TypeCode.Decimal => true,
            _ => false
        };
    }

    private string[]? GetCredentialTypes(PropertyInfo property)
    {
        var credAttr = property.GetCustomAttribute<CredentialMappingAttribute>();
        return credAttr?.CredentialTypes;
    }

    private DependencySchema? GetDependency(PropertyInfo property)
    {
        var depAttr = property.GetCustomAttribute<DependsOnAttribute>();
        if (depAttr == null)
            return null;

        return new DependencySchema
        {
            ParameterName = depAttr.ParameterName,
            ShowIf = depAttr.ShowIf
        };
    }

    private ValidationSchema? GetValidation(PropertyInfo property)
    {
        var validation = new ValidationSchema();
        bool hasValidation = false;

        // Range validation
        var rangeAttr = property.GetCustomAttribute<RangeAttribute>();
        if (rangeAttr != null)
        {
            validation.Min = Convert.ToDouble(rangeAttr.Minimum);
            validation.Max = Convert.ToDouble(rangeAttr.Maximum);
            hasValidation = true;
        }

        // String length validation
        var lengthAttr = property.GetCustomAttribute<StringLengthAttribute>();
        if (lengthAttr != null)
        {
            validation.MinLength = lengthAttr.MinimumLength;
            validation.MaxLength = lengthAttr.MaximumLength;
            hasValidation = true;
        }

        // Slider step
        var sliderAttr = property.GetCustomAttribute<SliderAttribute>();
        if (sliderAttr != null)
        {
            validation.Step = sliderAttr.Step;
            hasValidation = true;
        }

        return hasValidation ? validation : null;
    }

    private List<OptionSchema>? GetOptions(PropertyInfo property)
    {
        // Handle enum types
        var type = property.PropertyType;
        if (IsResolvableType(type))
            type = type.GetGenericArguments()[0];

        if (type.IsEnum)
        {
            var options = new List<OptionSchema>();
            foreach (var value in Enum.GetValues(type))
            {
                options.Add(new OptionSchema
                {
                    Label = value.ToString()!,
                    Value = value.ToString()!
                });
            }
            return options;
        }

        return null;
    }

    private bool HasAttribute<T>(PropertyInfo property) where T : Attribute
    {
        return property.GetCustomAttribute<T>() != null;
    }
}
