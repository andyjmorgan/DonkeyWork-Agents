using System.Reflection;
using DonkeyWork.Agents.Agents.Contracts.Nodes.Attributes;
using DonkeyWork.Agents.Agents.Contracts.Nodes.Enums;
using DonkeyWork.Agents.Agents.Contracts.Nodes.Schema;
using DonkeyWork.Agents.Agents.Contracts.Services;
using DonkeyWork.Agents.Common.Contracts.Enums;
using DonkeyWork.Agents.Common.Sdk.Types;
using DonkeyWork.Agents.Providers.Contracts.Configuration.Base;
using DonkeyWork.Agents.Providers.Contracts.Configuration.Provider;
using SdkConfigurableFieldAttribute = DonkeyWork.Agents.Common.Sdk.Attributes.ConfigurableFieldAttribute;
using SdkReliesUponAttribute = DonkeyWork.Agents.Common.Sdk.Attributes.ReliesUponAttribute;
using SdkSliderAttribute = DonkeyWork.Agents.Common.Sdk.Attributes.SliderAttribute;
using SdkRangeConstraintAttribute = DonkeyWork.Agents.Common.Sdk.Attributes.RangeConstraintAttribute;
using SdkSelectOptionsAttribute = DonkeyWork.Agents.Common.Sdk.Attributes.SelectOptionsAttribute;
using SdkEditorTypeAttribute = DonkeyWork.Agents.Common.Sdk.Attributes.EditorTypeAttribute;
using SdkCredentialMappingAttribute = DonkeyWork.Agents.Common.Sdk.Attributes.CredentialMappingAttribute;
using SdkRequiresCapabilityAttribute = DonkeyWork.Agents.Common.Sdk.Attributes.RequiresCapabilityAttribute;
using SdkTabAttribute = DonkeyWork.Agents.Common.Sdk.Attributes.TabAttribute;
using SdkEditorType = DonkeyWork.Agents.Common.Sdk.Attributes.EditorType;
using NodesReliesUponSchema = DonkeyWork.Agents.Agents.Contracts.Nodes.Schema.ReliesUponSchema;

namespace DonkeyWork.Agents.Agents.Core.Services;

/// <summary>
/// Service for generating multimodal chat model configuration schemas.
/// </summary>
public class MultimodalChatSchemaService : IMultimodalChatSchemaService
{
    private readonly Dictionary<LlmProvider, MultimodalChatSchema> _schemaCache = new();
    private readonly Dictionary<LlmProvider, Type> _providerConfigTypes = new()
    {
        { LlmProvider.OpenAI, typeof(OpenAIChatConfig) },
        { LlmProvider.Anthropic, typeof(AnthropicChatConfig) },
        { LlmProvider.Google, typeof(GoogleChatConfig) }
    };

    private readonly Dictionary<LlmProvider, string> _providerDisplayNames = new()
    {
        { LlmProvider.OpenAI, "OpenAI" },
        { LlmProvider.Anthropic, "Anthropic" },
        { LlmProvider.Google, "Google" }
    };

    /// <inheritdoc />
    public MultimodalChatSchema GenerateSchema(LlmProvider provider)
    {
        if (provider == LlmProvider.Unknown)
        {
            throw new ArgumentException("Cannot generate schema for Unknown provider", nameof(provider));
        }

        if (_schemaCache.TryGetValue(provider, out var cached))
        {
            return cached;
        }

        var schema = GenerateSchemaInternal(provider);
        _schemaCache[provider] = schema;
        return schema;
    }

    private MultimodalChatSchema GenerateSchemaInternal(LlmProvider provider)
    {
        var tabs = new Dictionary<string, TabSchema>();
        var fields = new List<FieldSchema>();

        // Generate fields from base ChatModelConfig
        GenerateFieldsFromType(typeof(ChatModelConfig), null, tabs, fields);

        // Generate fields from provider-specific config with prefix
        if (_providerConfigTypes.TryGetValue(provider, out var providerConfigType))
        {
            var providerTabName = _providerDisplayNames[provider];
            GenerateFieldsFromType(providerConfigType, "providerConfig", tabs, fields, providerTabName);
        }

        return new MultimodalChatSchema
        {
            Provider = provider,
            ProviderDisplayName = _providerDisplayNames.GetValueOrDefault(provider, provider.ToString()),
            Tabs = tabs.Values.OrderBy(t => t.Order).ToList(),
            Fields = fields.OrderBy(f => f.Tab).ThenBy(f => f.Order).ToList()
        };
    }

    private void GenerateFieldsFromType(
        Type configType,
        string? fieldPrefix,
        Dictionary<string, TabSchema> tabs,
        List<FieldSchema> fields,
        string? providerTabOverride = null)
    {
        var properties = configType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        foreach (var property in properties)
        {
            var fieldAttr = property.GetCustomAttribute<SdkConfigurableFieldAttribute>();
            if (fieldAttr == null)
            {
                continue;
            }

            var tabAttr = property.GetCustomAttribute<SdkTabAttribute>();
            var sliderAttr = property.GetCustomAttribute<SdkSliderAttribute>();
            var rangeAttr = property.GetCustomAttribute<SdkRangeConstraintAttribute>();
            var reliesUponAttr = property.GetCustomAttribute<SdkReliesUponAttribute>();
            var selectOptionsAttr = property.GetCustomAttribute<SdkSelectOptionsAttribute>();
            var editorTypeAttr = property.GetCustomAttribute<SdkEditorTypeAttribute>();
            var credentialMappingAttr = property.GetCustomAttribute<SdkCredentialMappingAttribute>();
            var requiresCapabilityAttr = property.GetCustomAttribute<SdkRequiresCapabilityAttribute>();
            var immutableAttr = property.GetCustomAttribute<ImmutableAttribute>();
            var supportedByAttr = property.GetCustomAttribute<SupportedByAttribute>();
            var groupAttr = property.GetCustomAttribute<GroupAttribute>();

            // Determine tab name - use provider override for provider-specific fields without explicit tab
            var tabName = providerTabOverride ?? tabAttr?.Name;

            // Add tab if not already present
            if (!string.IsNullOrEmpty(tabName) && !tabs.ContainsKey(tabName))
            {
                tabs[tabName] = new TabSchema
                {
                    Name = tabName,
                    Order = tabAttr?.Order ?? 100,
                    Icon = tabAttr?.Icon
                };
            }

            // Determine field name with prefix
            var fieldName = ToCamelCase(property.Name);
            if (!string.IsNullOrEmpty(fieldPrefix))
            {
                fieldName = $"{fieldPrefix}.{fieldName}";
            }

            // Determine control type
            var controlType = DetermineControlType(property.PropertyType, sliderAttr, rangeAttr, selectOptionsAttr, editorTypeAttr, credentialMappingAttr);

            // Get default value
            object? defaultValue = null;
            if (sliderAttr?.HasDefault == true)
            {
                defaultValue = sliderAttr.Default;
            }
            else if (rangeAttr?.HasDefault == true)
            {
                defaultValue = rangeAttr.Default;
            }
            else if (selectOptionsAttr?.Default != null)
            {
                defaultValue = selectOptionsAttr.Default;
            }

            // Build reliesUpon with prefix if needed
            NodesReliesUponSchema? reliesUponSchema = null;
            if (reliesUponAttr != null)
            {
                var dependsOnFieldName = ToCamelCase(reliesUponAttr.FieldName);
                // If this is a provider-specific field, prefix the dependency field name too
                if (!string.IsNullOrEmpty(fieldPrefix))
                {
                    dependsOnFieldName = $"{fieldPrefix}.{dependsOnFieldName}";
                }

                reliesUponSchema = new NodesReliesUponSchema
                {
                    FieldName = dependsOnFieldName,
                    Value = reliesUponAttr.Value,
                    RequiredWhenEnabled = reliesUponAttr.RequiredWhenEnabled
                };
            }

            // Determine group from attribute or field attribute
            var group = groupAttr?.Name ?? fieldAttr.Group;

            var fieldSchema = new FieldSchema
            {
                Name = fieldName,
                Label = fieldAttr.Label,
                Description = fieldAttr.Description,
                PropertyType = GetPropertyTypeName(property.PropertyType),
                ControlType = controlType,
                Order = fieldAttr.Order,
                Tab = tabName,
                Required = fieldAttr.Required || IsRequiredProperty(property),
                SupportsVariables = IsResolvableType(property.PropertyType),
                Placeholder = fieldAttr.Placeholder,
                DefaultValue = defaultValue,
                Min = sliderAttr?.Min ?? rangeAttr?.Min,
                Max = sliderAttr?.Max ?? rangeAttr?.Max,
                Step = sliderAttr?.Step,
                Options = GetEnumOptions(property.PropertyType) ?? selectOptionsAttr?.Options,
                ReliesUpon = reliesUponSchema,
                Immutable = immutableAttr != null,
                SupportedBy = supportedByAttr?.ModelIds,
                Group = group
            };

            fields.Add(fieldSchema);
        }
    }

    private static ControlType DetermineControlType(
        Type propertyType,
        SdkSliderAttribute? sliderAttr,
        SdkRangeConstraintAttribute? rangeAttr,
        SdkSelectOptionsAttribute? selectOptionsAttr,
        SdkEditorTypeAttribute? editorTypeAttr,
        SdkCredentialMappingAttribute? credentialMappingAttr)
    {
        // Explicit editor type takes precedence
        if (editorTypeAttr != null)
        {
            return editorTypeAttr.EditorType switch
            {
                SdkEditorType.Text => ControlType.Text,
                SdkEditorType.TextArea => ControlType.TextArea,
                SdkEditorType.TextAreaList => ControlType.TextAreaList,
                SdkEditorType.Code => ControlType.Code,
                SdkEditorType.Dropdown => ControlType.Select,
                SdkEditorType.Number => ControlType.Number,
                SdkEditorType.Checkbox => ControlType.Toggle,
                SdkEditorType.Slider => ControlType.Slider,
                SdkEditorType.Json => ControlType.Json,
                SdkEditorType.KeyValueList => ControlType.KeyValue,
                SdkEditorType.Credential => ControlType.Credential,
                _ => ControlType.Text
            };
        }

        // Credential mapping
        if (credentialMappingAttr != null)
        {
            return ControlType.Credential;
        }

        // Slider attribute
        if (sliderAttr != null)
        {
            return ControlType.Slider;
        }

        // Range constraint = number input
        if (rangeAttr != null)
        {
            return ControlType.Number;
        }

        // Select options
        if (selectOptionsAttr != null)
        {
            return ControlType.Select;
        }

        // Infer from type
        var underlyingType = GetUnderlyingType(propertyType);

        if (underlyingType == typeof(bool))
        {
            return ControlType.Toggle;
        }

        if (underlyingType == typeof(int) || underlyingType == typeof(long) ||
            underlyingType == typeof(double) || underlyingType == typeof(float) ||
            underlyingType == typeof(decimal))
        {
            return ControlType.Number;
        }

        if (underlyingType.IsEnum)
        {
            return ControlType.Select;
        }

        if (underlyingType == typeof(Guid))
        {
            return ControlType.Text;
        }

        // Check for array types (Resolvable<string>[])
        if (propertyType.IsArray && propertyType.GetElementType() != null)
        {
            return ControlType.TextAreaList;
        }

        return ControlType.Text;
    }

    private static Type GetUnderlyingType(Type type)
    {
        // Handle nullable
        var nullableUnderlying = Nullable.GetUnderlyingType(type);
        if (nullableUnderlying != null)
        {
            type = nullableUnderlying;
        }

        // Handle Resolvable<T>
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Resolvable<>))
        {
            return type.GetGenericArguments()[0];
        }

        return type;
    }

    private static bool IsResolvableType(Type type)
    {
        // Handle nullable
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        // Check for Resolvable<T>
        if (underlyingType.IsGenericType && underlyingType.GetGenericTypeDefinition() == typeof(Resolvable<>))
        {
            return true;
        }

        // Check for Resolvable<T>[]
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

    private static string GetPropertyTypeName(Type type)
    {
        var underlyingType = GetUnderlyingType(type);

        if (underlyingType == typeof(string))
            return "string";
        if (underlyingType == typeof(int) || underlyingType == typeof(long) ||
            underlyingType == typeof(double) || underlyingType == typeof(float) ||
            underlyingType == typeof(decimal))
            return "number";
        if (underlyingType == typeof(bool))
            return "boolean";
        if (underlyingType == typeof(Guid))
            return "string";
        if (underlyingType.IsEnum)
            return "enum";
        if (type.IsArray)
            return "array";

        return "object";
    }

    private static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;
        return char.ToLowerInvariant(name[0]) + name[1..];
    }

    private static bool IsRequiredProperty(PropertyInfo property)
    {
        var nullabilityContext = new NullabilityInfoContext();
        var nullabilityInfo = nullabilityContext.Create(property);
        return nullabilityInfo.WriteState == NullabilityState.NotNull;
    }

    private static IReadOnlyList<string>? GetEnumOptions(Type type)
    {
        var underlyingType = GetUnderlyingType(type);
        if (!underlyingType.IsEnum)
            return null;

        return Enum.GetNames(underlyingType);
    }
}
