using System.Reflection;
using DonkeyWork.Agents.Common.Contracts.Enums;
using DonkeyWork.Agents.Providers.Contracts.Attributes;
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

            var fields = BuildFieldSchemas(configType, model);

            schemas[model.Id] = new ModelConfigSchema
            {
                ModelId = model.Id,
                ModelName = model.Name,
                Provider = model.Provider,
                Mode = model.Mode,
                Fields = fields
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
            ModelMode.Chat => typeof(ChatModelConfig),
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

    private static IReadOnlyList<ConfigFieldSchema> BuildFieldSchemas(Type configType, ModelDefinition model)
    {
        var fields = new List<ConfigFieldSchema>();
        var properties = configType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            var configFieldAttr = property.GetCustomAttribute<ConfigFieldAttribute>();
            if (configFieldAttr == null)
            {
                continue;
            }

            // Check capability requirements
            if (!HasRequiredCapabilities(property, model.Supports))
            {
                continue;
            }

            // Check if field is hidden via override
            if (model.ConfigOverrides != null &&
                model.ConfigOverrides.TryGetValue(ToCamelCase(property.Name), out var fieldOverride) &&
                fieldOverride.Hidden == true)
            {
                continue;
            }

            var fieldSchema = BuildFieldSchema(property, configFieldAttr, model.ConfigOverrides);
            if (fieldSchema != null)
            {
                fields.Add(fieldSchema);
            }
        }

        return fields.OrderBy(f => f.Order).ToList();
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
        ConfigFieldAttribute configFieldAttr,
        IReadOnlyDictionary<string, FieldOverride>? overrides)
    {
        var fieldName = ToCamelCase(property.Name);
        var sliderAttr = property.GetCustomAttribute<SliderAttribute>();
        var rangeAttr = property.GetCustomAttribute<RangeConstraintAttribute>();
        var selectAttr = property.GetCustomAttribute<SelectAttribute>();

        // Determine control type
        var controlType = DetermineControlType(property, sliderAttr, rangeAttr, selectAttr);
        var propertyType = GetPropertyTypeName(property.PropertyType);

        // Get override values if present
        FieldOverride? fieldOverride = null;
        overrides?.TryGetValue(fieldName, out fieldOverride);

        // Build base schema from attributes
        double? min = null, max = null, step = null;
        object? defaultValue = null;
        IReadOnlyList<string>? options = null;
        IReadOnlyList<FieldDependency>? dependencies = null;

        // Read dependencies
        var dependsOnAttrs = property.GetCustomAttributes<DependsOnAttribute>();
        if (dependsOnAttrs.Any())
        {
            dependencies = dependsOnAttrs.Select(attr => new FieldDependency
            {
                Field = ToCamelCase(attr.FieldName),
                Value = FormatDefaultValue(attr.Value) ?? attr.Value
            }).ToList();
        }

        if (sliderAttr != null)
        {
            min = fieldOverride?.Min ?? sliderAttr.Min;
            max = fieldOverride?.Max ?? sliderAttr.Max;
            step = fieldOverride?.Step ?? sliderAttr.Step;
            defaultValue = fieldOverride?.Default ?? (sliderAttr.HasDefaultValue ? sliderAttr.DefaultValue : null);
        }
        else if (rangeAttr != null)
        {
            min = fieldOverride?.Min ?? rangeAttr.Min;
            max = fieldOverride?.Max ?? rangeAttr.Max;
            defaultValue = fieldOverride?.Default ?? (rangeAttr.HasDefaultValue ? rangeAttr.DefaultValue : null);
        }
        else if (selectAttr != null)
        {
            defaultValue = fieldOverride?.Default ?? selectAttr.DefaultValue;
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
            Min = min,
            Max = max,
            Step = step,
            DefaultValue = FormatDefaultValue(defaultValue),
            Options = options,
            DependsOn = dependencies
        };
    }

    private static FieldControlType DetermineControlType(
        PropertyInfo property,
        SliderAttribute? sliderAttr,
        RangeConstraintAttribute? rangeAttr,
        SelectAttribute? selectAttr)
    {
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

        var underlyingType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

        if (underlyingType == typeof(bool))
        {
            return FieldControlType.Toggle;
        }

        if (underlyingType == typeof(int) || underlyingType == typeof(double) || underlyingType == typeof(float))
        {
            return FieldControlType.NumberInput;
        }

        return FieldControlType.TextInput;
    }

    private static string GetPropertyTypeName(Type type)
    {
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        if (underlyingType.IsEnum)
        {
            return "string";
        }

        return underlyingType.Name.ToLowerInvariant() switch
        {
            "int32" => "int32",
            "int64" => "int64",
            "double" => "double",
            "single" => "float",
            "boolean" => "boolean",
            "string" => "string",
            _ => underlyingType.Name.ToLowerInvariant()
        };
    }

    private static bool IsEnumType(Type type)
    {
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;
        return underlyingType.IsEnum;
    }

    private static IReadOnlyList<string>? GetEnumOptions(Type type)
    {
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;
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
