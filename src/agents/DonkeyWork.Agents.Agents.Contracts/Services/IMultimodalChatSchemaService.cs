using DonkeyWork.Agents.Agents.Contracts.Nodes.Schema;
using DonkeyWork.Agents.Common.Contracts.Enums;

namespace DonkeyWork.Agents.Agents.Contracts.Services;

/// <summary>
/// Service for generating multimodal chat model configuration schemas.
/// </summary>
public interface IMultimodalChatSchemaService
{
    /// <summary>
    /// Generates a configuration schema for the specified LLM provider.
    /// Includes base fields from ChatModelConfig and provider-specific fields.
    /// </summary>
    /// <param name="provider">The LLM provider to generate schema for.</param>
    /// <returns>A schema containing tabs and fields for the configuration UI.</returns>
    MultimodalChatSchema GenerateSchema(LlmProvider provider);
}
