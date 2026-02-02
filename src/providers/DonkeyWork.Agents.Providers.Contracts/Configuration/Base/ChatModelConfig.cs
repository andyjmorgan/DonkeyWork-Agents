using DonkeyWork.Agents.Common.Sdk.Attributes;
using DonkeyWork.Agents.Common.Sdk.Models;
using DonkeyWork.Agents.Common.Sdk.Types;

namespace DonkeyWork.Agents.Providers.Contracts.Configuration.Base;

/// <summary>
/// Abstract base configuration for chat/completion models.
/// Provider-specific configurations should inherit from this class.
/// </summary>
public abstract class ChatModelConfig : BaseConfigurableParameters, IModelConfig
{
    // === Credential (Basic Tab) ===
    [ConfigurableField(Label = "Credential", Order = 0, Required = true)]
    [Tab("Basic", Order = 0)]
    [CredentialMapping("Anthropic", "OpenAI", "Google", "Azure")]
    public Resolvable<Guid>? CredentialId { get; init; }

    // === Prompts (Basic Tab, Prompts Group) ===
    [ConfigurableField(Label = "System Prompts", Description = "Instructions that define the AI's behavior and role", Order = 10, Group = "Prompts")]
    [Tab("Basic")]
    [EditorType(EditorType.TextAreaList)]
    public Resolvable<string>[]? SystemPrompts { get; init; }

    [ConfigurableField(Label = "User Messages", Description = "Use {{...}} for variables (e.g., {{start.input}})", Order = 20, Group = "Prompts")]
    [Tab("Basic")]
    [EditorType(EditorType.TextAreaList)]
    public Resolvable<string>[]? UserMessages { get; init; }

    // === Sampling Parameters (Basic Tab, Sampling Group) ===
    [ConfigurableField(Label = "Temperature", Description = "Controls randomness (0=deterministic, higher=creative)", Order = 30, Group = "Sampling")]
    [Tab("Basic")]
    [Slider(Min = 0, Max = 2, Step = 0.1, Default = 1.0)]
    public Resolvable<double>? Temperature { get; init; }

    [ConfigurableField(Label = "Max Output Tokens", Description = "Maximum tokens to generate (actual max depends on selected model)", Order = 40, Group = "Sampling")]
    [Tab("Basic")]
    [Slider(Min = 1, Max = 128000, Step = 256, Default = 4096)]
    public Resolvable<int>? MaxOutputTokens { get; init; }

    // === Streaming (Basic Tab) ===
    [ConfigurableField(Label = "Stream Output", Description = "Stream the response as it's generated", Order = 50)]
    [Tab("Basic")]
    public Resolvable<bool>? Stream { get; init; }
}
