using DonkeyWork.Agents.Providers.Contracts.Attributes;
using DonkeyWork.Agents.Providers.Contracts.Configuration.Base;

namespace DonkeyWork.Agents.Providers.Contracts.Configuration.Examples;

/// <summary>
/// Example configuration demonstrating conditional field visibility using DependsOn.
/// This class is for documentation purposes and is not used in production.
/// </summary>
public class ConditionalFieldsExample : IModelConfig
{
    // Base toggle field
    [ConfigField(Label = "Enable Caching", Description = "Use prompt caching to reduce costs", Order = 10)]
    public bool? EnableCaching { get; init; }

    // This field only appears if EnableCaching is true
    [ConfigField(Label = "Cache Size", Description = "Maximum cache size in MB", Order = 11)]
    [DependsOn(FieldName = nameof(EnableCaching), Value = true)]
    [RangeConstraint(Min = 100, Max = 10000, DefaultValue = 1000)]
    public int? CacheSize { get; init; }

    // Multiple dependencies (both must be true)
    [ConfigField(Label = "Cache Expiration", Description = "Cache TTL in seconds", Order = 12)]
    [DependsOn(FieldName = nameof(EnableCaching), Value = true)]
    [RangeConstraint(Min = 60, Max = 86400, DefaultValue = 3600)]
    public int? CacheExpiration { get; init; }

    // Another base field (enum)
    [ConfigField(Label = "Output Format", Description = "Response format", Order = 20)]
    [Select(DefaultValue = nameof(OutputFormat.Json))]
    public OutputFormat? Format { get; init; }

    // Depends on enum value
    [ConfigField(Label = "Pretty Print", Description = "Format JSON with indentation", Order = 21)]
    [DependsOn(FieldName = nameof(Format), Value = OutputFormat.Json)]
    public bool? PrettyPrint { get; init; }
}

public enum OutputFormat
{
    Json,
    Xml,
    Text
}
