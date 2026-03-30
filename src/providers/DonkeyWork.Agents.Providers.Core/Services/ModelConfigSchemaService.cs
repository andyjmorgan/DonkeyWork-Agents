using System.Reflection;
using DonkeyWork.Agents.Common.Contracts.Enums;
using DonkeyWork.Agents.Common.Sdk.Attributes;
using DonkeyWork.Agents.Common.Sdk.Models.Schema;
using DonkeyWork.Agents.Common.Sdk.Types;
using DonkeyWork.Agents.Providers.Contracts.Configuration.Base;
using DonkeyWork.Agents.Providers.Contracts.Configuration.Provider;
using DonkeyWork.Agents.Providers.Contracts.Enums;
using DonkeyWork.Agents.Providers.Contracts.Models;
using DonkeyWork.Agents.Providers.Contracts.Models.Schema;
using DonkeyWork.Agents.Providers.Contracts.Services;

namespace DonkeyWork.Agents.Providers.Core.Services;

/// <summary>
/// Service for generating configuration schemas for models by reading attributes from config classes.
/// </summary>
public sealed class ModelConfigSchemaService : IModelConfigSchemaService
{
    private readonly IModelCatalogService _modelCatalogService;
    private readonly Lazy<IReadOnlyDictionary<string, ModelConfigSchema>> _allSchemas;

    public ModelConfigSchemaService(IModelCatalogService modelCatalogService)
    {
        _modelCatalogService = modelCatalogService;
        _allSchemas = new Lazy<IReadOnlyDictionary<string, ModelConfigSchema>>(BuildAllSchemas);
    }

    public ModelConfigSchema? GetSchemaForModel(string modelId)
    {
        _allSchemas.Value.TryGetValue(modelId, out var schema);
        return schema;
    }

    public IReadOnlyDictionary<string, ModelConfigSchema> GetAllSchemas()
    {
        return _allSchemas.Value;
    }

    private IReadOnlyDictionary<string, ModelConfigSchema> BuildAllSchemas()
    {
        var models = _modelCatalogService.GetAllModels();
        var schemas = new Dictionary<string, ModelConfigSchema>();

        foreach (var model in models)
        {
            var configType = GetConfigTypeForModel(model);
            if (configType == null)
            {
                continue;
            }

            var (fields, tabs) = BuildFieldAndTabSchemas(configType, model);

            schemas[model.Id] = new ModelConfigSchema
            {
                ModelId = model.Id,
                ModelName = model.Name,
                Provider = model.Provider,
                Mode = model.Mode,
                Fields = fields,
                Tabs = tabs
            };
        }

        return schemas;
    }

    private static Type? GetConfigTypeForModel(ModelDefinition model)
    {
        // First check for provider-specific config
        var providerConfig = GetProviderSpecificConfigType(model.Provider, model.Mode);
        if (providerConfig != null)
        {
            return providerConfig;
        }

        // Fall back to base config for mode
        return model.Mode switch
        {
            ModelMode.Chat => typeof(AnthropicChatConfig), // Use a concrete implementation as default
            ModelMode.ImageGeneration => typeof(ImageGenerationConfig),
            ModelMode.AudioGeneration => typeof(AudioGenerationConfig),
            _ => null
        };
    }

    private static Type? GetProviderSpecificConfigType(LlmProvider provider, ModelMode mode)
    {
        return (provider, mode) switch
        {
            (LlmProvider.Anthropic, ModelMode.Chat) => typeof(AnthropicChatConfig),
            (LlmProvider.OpenAI, ModelMode.Chat) => typeof(OpenAIChatConfig),
            (LlmProvider.Google, ModelMode.Chat) => typeof(GoogleChatConfig),
            _ => null
        };
    }

    private static (IReadOnlyList<ConfigFieldSchema> Fields, IReadOnlyList<TabSchema> Tabs) BuildFieldAndTabSchemas(
        Type configType,
        ModelDefinition model)
    {
        var fields = new List<ConfigFieldSchema>();
        var tabsDict = new Dictionary<string, TabSchema>();
        var properties = configType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            var configFieldAttr = property.GetCustomAttribute<ConfigurableFieldAttribute>();
            if (configFieldAttr == null)
            {
                continue;
            }

            if (!HasRequiredCapabilities(property, model.Supports))
            {
                continue;
            }

            if (model.ConfigOverrides != null &&
                model.ConfigOverrides.TryGetValue(ToCamelCase(property.Name), out var fieldOverride) &&
                fieldOverride.Hidden == true)
            {
                continue;
            }

            var tabAttr = property.GetCustomAttribute<TabAttribute>();
            if (tabAttr != null && !tabsDict.ContainsKey(tabAttr.Name))
            {
                tabsDict[tabAttr.Name] = new TabSchema
                {
                    Name = tabAttr.Name,
                    Order = tabAttr.Order,
                    Icon = tabAttr.Icon
                };
            }

            var fieldSchema = BuildFieldSchema(property, configFieldAttr, model.ConfigOverrides);
            if (fieldSchema != null)
            {
                fields.Add(fieldSchema);
            }
        }

        var orderedTabs = tabsDict.Values.OrderBy(t => t.Order).ToList();
        var orderedFields = fields.OrderBy(f => f.Order).ToList();

        return (orderedFields, orderedTabs);
    }

    private static bool HasRequiredCapabilities(PropertyInfo property, ModelSupports supports)
    {
        var capabilityAttrs = property.GetCustomAttributes<RequiresCapabilityAttribute>();

        foreach (var attr in capabilityAttrs)
        {
            var capabilityProp = typeof(ModelSupports).GetProperty(attr.Capability);
            if (capabilityProp == null)
            {
                return false;
            }

            var value = capabilityProp.GetValue(supports);
            if (value is not true)
            {
                return false;
            }
        }

        return true;
    }

    private static ConfigFieldSchema? BuildFieldSchema(
        PropertyInfo property,
        ConfigurableFieldAttribute configFieldAttr,
        IReadOnlyDictionary<string, FieldOverride>? overrides)
    {
        var fieldName = ToCamelCase(property.Name);
        var sliderAttr = property.GetCustomAttribute<SliderAttribute>();
        var rangeAttr = property.GetCustomAttribute<RangeConstraintAttribute>();
        var selectAttr = property.GetCustomAttribute<SelectOptionsAttribute>();
        var tabAttr = property.GetCustomAttribute<TabAttribute>();
        var editorTypeAttr = property.GetCustomAttribute<EditorTypeAttribute>();
        var credentialAttr = property.GetCustomAttribute<CredentialMappingAttribute>();
        var reliesUponAttr = property.GetCustomAttribute<ReliesUponAttribute>();

        // Determine control type
        var controlType = DetermineControlType(property, sliderAttr, rangeAttr, selectAttr, editorTypeAttr, credentialAttr);
        var propertyType = GetPropertyTypeName(property.PropertyType);
        var isResolvable = IsResolvableType(property.PropertyType);

        FieldOverride? fieldOverride = null;
        overrides?.TryGetValue(fieldName, out fieldOverride);

        double? min = null, max = null, step = null;
        object? defaultValue = null;
        IReadOnlyList<string>? options = null;
        IReadOnlyList<FieldDependency>? dependencies = null;

        ReliesUponSchema? reliesUponSchema = null;
        if (reliesUponAttr != null)
        {
            reliesUponSchema = new ReliesUponSchema
            {
                FieldName = ToCamelCase(reliesUponAttr.FieldName),
                Value = FormatDefaultValue(reliesUponAttr.Value) ?? reliesUponAttr.Value,
                RequiredWhenEnabled = reliesUponAttr.RequiredWhenEnabled
            };
        }

        var dependsOnAttrs = property.GetCustomAttributes<DependsOnAttribute>();
        if (dependsOnAttrs.Any())
        {
            dependencies = dependsOnAttrs.Select(attr => new FieldDependency
            {
                Field = ToCamelCase(attr.ParameterName),
                Value = (object?)attr.ShowIf ?? true
            }).ToList();
        }

        if (sliderAttr != null)
        {
            min = fieldOverride?.Min ?? sliderAttr.Min;
            max = fieldOverride?.Max ?? sliderAttr.Max;
            step = fieldOverride?.Step ?? sliderAttr.Step;
            defaultValue = fieldOverride?.Default ?? (sliderAttr.HasDefault ? sliderAttr.Default : null);
        }
        else if (rangeAttr != null)
        {
            min = fieldOverride?.Min ?? rangeAttr.Min;
            max = fieldOverride?.Max ?? rangeAttr.Max;
            defaultValue = fieldOverride?.Default ?? (rangeAttr.HasDefault ? rangeAttr.Default : null);
        }
        else if (selectAttr != null)
        {
            defaultValue = fieldOverride?.Default ?? selectAttr.Default;
            options = selectAttr.Options ?? GetEnumOptions(property.PropertyType);
        }
        else if (IsEnumType(property.PropertyType))
        {
            options = GetEnumOptions(property.PropertyType);
        }

        return new ConfigFieldSchema
        {
            Name = fieldName,
            Label = configFieldAttr.Label,
            Description = configFieldAttr.Description,
            ControlType = controlType,
            PropertyType = propertyType,
            Order = configFieldAttr.Order,
            Group = configFieldAttr.Group,
            Tab = tabAttr?.Name,
            Required = configFieldAttr.Required,
            Resolvable = isResolvable,
            Min = min,
            Max = max,
            Step = step,
            DefaultValue = FormatDefaultValue(defaultValue),
            Options = options,
            DependsOn = dependencies,
            ReliesUpon = reliesUponSchema,
            CredentialTypes = credentialAttr?.CredentialTypes
        };
    }

    private static bool IsResolvableType(Type type)
    {
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        if (underlyingType.IsGenericType &&
            underlyingType.GetGenericTypeDefinition() == typeof(Resolvable<>))
        {
            return true;
        }

        if (underlyingType.IsArray)
        {
            var elementType = underlyingType.GetElementType();
            if (elementType != null && elementType.IsGenericType &&
                elementType.GetGenericTypeDefinition() == typeof(Resolvable<>))
            {
                return true;
            }
        }

        return false;
    }

    private static FieldControlType DetermineControlType(
        PropertyInfo property,
        SliderAttribute? sliderAttr,
        RangeConstraintAttribute? rangeAttr,
        SelectOptionsAttribute? selectAttr,
        EditorTypeAttribute? editorTypeAttr,
        CredentialMappingAttribute? credentialAttr)
    {
        // Explicit editor type takes precedence
        if (editorTypeAttr != null)
        {
            return editorTypeAttr.EditorType switch
            {
                EditorType.Slider => FieldControlType.Slider,
                EditorType.Dropdown => FieldControlType.Select,
                EditorType.TextArea => FieldControlType.TextArea,
                EditorType.Checkbox => FieldControlType.Toggle,
                EditorType.Number => FieldControlType.NumberInput,
                EditorType.Credential => FieldControlType.Credential,
                EditorType.Code => FieldControlType.Code,
                EditorType.Json => FieldControlType.Json,
                EditorType.KeyValueList => FieldControlType.KeyValueList,
                _ => FieldControlType.TextInput
            };
        }

        // Credential mapping
        if (credentialAttr != null)
        {
            return FieldControlType.Credential;
        }

        if (sliderAttr != null)
        {
            return FieldControlType.Slider;
        }

        if (selectAttr != null || IsEnumType(property.PropertyType))
        {
            return FieldControlType.Select;
        }

        if (rangeAttr != null)
        {
            return FieldControlType.NumberInput;
        }

        var underlyingType = GetUnderlyingPropertyType(property.PropertyType);

        if (underlyingType == typeof(bool))
        {
            return FieldControlType.Toggle;
        }

        if (underlyingType == typeof(int) || underlyingType == typeof(double) || underlyingType == typeof(float))
        {
            return FieldControlType.NumberInput;
        }

        // Array types default to text area for string arrays
        if (property.PropertyType.IsArray)
        {
            return FieldControlType.TextArea;
        }

        return FieldControlType.TextInput;
    }

    private static Type GetUnderlyingPropertyType(Type type)
    {
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        if (underlyingType.IsGenericType &&
            underlyingType.GetGenericTypeDefinition() == typeof(Resolvable<>))
        {
            underlyingType = underlyingType.GetGenericArguments()[0];
        }

        if (underlyingType.IsArray)
        {
            var elementType = underlyingType.GetElementType();
            if (elementType != null)
            {
                if (elementType.IsGenericType &&
                    elementType.GetGenericTypeDefinition() == typeof(Resolvable<>))
                {
                    return elementType.GetGenericArguments()[0];
                }
                return elementType;
            }
        }

        return underlyingType;
    }

    private static string GetPropertyTypeName(Type type)
    {
        var underlyingType = GetUnderlyingPropertyType(type);

        if (underlyingType.IsEnum)
        {
            return "string";
        }

        var isArray = type.IsArray ||
                     (Nullable.GetUnderlyingType(type) ?? type).IsArray ||
                     (type.IsGenericType &&
                      type.GetGenericTypeDefinition() == typeof(Resolvable<>) &&
                      type.GetGenericArguments()[0].IsArray);

        var baseType = underlyingType.Name.ToLowerInvariant() switch
        {
            "int32" => "int32",
            "int64" => "int64",
            "double" => "double",
            "single" => "float",
            "boolean" => "boolean",
            "string" => "string",
            "guid" => "guid",
            _ => underlyingType.Name.ToLowerInvariant()
        };

        return isArray ? $"{baseType}[]" : baseType;
    }

    private static bool IsEnumType(Type type)
    {
        var underlyingType = GetUnderlyingPropertyType(type);
        return underlyingType.IsEnum;
    }

    private static IReadOnlyList<string>? GetEnumOptions(Type type)
    {
        var underlyingType = GetUnderlyingPropertyType(type);
        if (!underlyingType.IsEnum)
        {
            return null;
        }

        return Enum.GetNames(underlyingType);
    }

    private static object? FormatDefaultValue(object? value)
    {
        if (value == null)
        {
            return null;
        }

        if (value.GetType().IsEnum)
        {
            return value.ToString();
        }

        return value;
    }

    private static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        return char.ToLowerInvariant(name[0]) + name[1..];
    }
}
