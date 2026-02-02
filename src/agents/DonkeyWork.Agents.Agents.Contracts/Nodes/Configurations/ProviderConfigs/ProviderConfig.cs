using System.Text.Json.Serialization;
using DonkeyWork.Agents.Common.Contracts.Enums;

namespace DonkeyWork.Agents.Agents.Contracts.Nodes.Configurations.ProviderConfigs;

/// <summary>
/// Base class for provider-specific configuration.
/// Derived types are registered for polymorphic serialization.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(OpenAIProviderConfig), nameof(LlmProvider.OpenAI))]
[JsonDerivedType(typeof(AnthropicProviderConfig), nameof(LlmProvider.Anthropic))]
[JsonDerivedType(typeof(GoogleProviderConfig), nameof(LlmProvider.Google))]
public abstract class ProviderConfig
{
}
