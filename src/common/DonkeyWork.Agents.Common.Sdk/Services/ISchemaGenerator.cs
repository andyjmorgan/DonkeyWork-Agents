using DonkeyWork.Agents.Common.Sdk.Models.Schema;

namespace DonkeyWork.Agents.Common.Sdk.Services;

/// <summary>
/// Service for generating configuration schemas from attributed types.
/// </summary>
public interface ISchemaGenerator
{
    /// <summary>
    /// Generates a schema for the specified configuration type.
    /// </summary>
    ConfigurationSchema GenerateSchema(Type configurationType);

    /// <summary>
    /// Generates a schema for the specified configuration type.
    /// </summary>
    ConfigurationSchema GenerateSchema<T>();
}

/// <summary>
/// Complete schema for a configuration type including tabs and fields.
/// </summary>
public sealed class ConfigurationSchema
{
    /// <summary>
    /// The name of the configuration type.
    /// </summary>
    public required string TypeName { get; init; }

    /// <summary>
    /// Available tabs for organizing fields.
    /// </summary>
    public IReadOnlyList<TabSchema> Tabs { get; init; } = [];

    /// <summary>
    /// All configurable fields.
    /// </summary>
    public IReadOnlyList<FieldSchema> Fields { get; init; } = [];
}
