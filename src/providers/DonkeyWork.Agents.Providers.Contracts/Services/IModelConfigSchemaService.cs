using DonkeyWork.Agents.Providers.Contracts.Models.Schema;

namespace DonkeyWork.Agents.Providers.Contracts.Services;

/// <summary>
/// Service for generating configuration schemas for models.
/// </summary>
public interface IModelConfigSchemaService
{
    /// <summary>
    /// Gets the configuration schema for a specific model.
    /// </summary>
    /// <param name="modelId">The model identifier.</param>
    /// <returns>The model's configuration schema, or null if the model is not found.</returns>
    ModelConfigSchema? GetSchemaForModel(string modelId);

    /// <summary>
    /// Gets configuration schemas for all available models.
    /// </summary>
    /// <returns>A dictionary mapping model IDs to their configuration schemas.</returns>
    IReadOnlyDictionary<string, ModelConfigSchema> GetAllSchemas();
}
