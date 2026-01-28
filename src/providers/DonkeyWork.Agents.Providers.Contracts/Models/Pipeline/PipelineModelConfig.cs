using DonkeyWork.Agents.Common.Contracts.Enums;

namespace DonkeyWork.Agents.Providers.Contracts.Models.Pipeline;

/// <summary>
/// Configuration for the model to use in the pipeline.
/// </summary>
public class PipelineModelConfig
{
    /// <summary>
    /// The LLM provider.
    /// </summary>
    public required LlmProvider Provider { get; set; }

    /// <summary>
    /// The model identifier.
    /// </summary>
    public required string ModelId { get; set; }

    /// <summary>
    /// The decrypted API key for the provider.
    /// </summary>
    public required string ApiKey { get; set; }
}
