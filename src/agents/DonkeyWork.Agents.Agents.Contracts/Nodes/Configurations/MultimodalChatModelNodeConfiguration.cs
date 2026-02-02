using System.Text.Json.Serialization;
using DonkeyWork.Agents.Agents.Contracts.Nodes.Attributes;
using DonkeyWork.Agents.Agents.Contracts.Nodes.Configurations.ProviderConfigs;
using DonkeyWork.Agents.Agents.Contracts.Nodes.Enums;
using DonkeyWork.Agents.Common.Contracts.Enums;

namespace DonkeyWork.Agents.Agents.Contracts.Nodes.Configurations;

/// <summary>
/// Configuration for the Multimodal Chat node - chat with a multimodal LLM.
/// </summary>
[Node(
    DisplayName = "Multimodal Chat",
    Description = "Chat with a multimodal LLM",
    Category = "AI",
    Icon = "brain",
    Color = "blue")]
public sealed class MultimodalChatModelNodeConfiguration : NodeConfiguration, IRequiresCredential
{
    /// <inheritdoc />
    public override NodeType NodeType => NodeType.MultimodalChatModel;

    /// <summary>
    /// The LLM provider to use. Set on drag, read-only in properties panel.
    /// </summary>
    [JsonPropertyName("provider")]
    [Immutable]
    public required LlmProvider Provider { get; init; }

    /// <summary>
    /// The model ID to use (e.g., "gpt-4o", "claude-3-opus"). Set on drag, read-only in properties panel.
    /// </summary>
    [JsonPropertyName("modelId")]
    [Immutable]
    public required string ModelId { get; init; }

    /// <summary>
    /// The credential ID for authenticating with the LLM provider.
    /// </summary>
    [JsonPropertyName("credentialId")]
    [ConfigurableField(Label = "Credential", ControlType = ControlType.Credential, Order = 10)]
    [Tab("Basic", Order = 1, Icon = "settings")]
    public required Guid CredentialId { get; init; }

    /// <summary>
    /// Sampling temperature (0-2). Higher values make output more random.
    /// </summary>
    [JsonPropertyName("temperature")]
    [ConfigurableField(Label = "Temperature", ControlType = ControlType.Slider, Order = 20)]
    [Tab("Basic")]
    [Slider(Min = 0, Max = 2, Step = 0.1, Default = 1.0)]
    public double? Temperature { get; init; }

    /// <summary>
    /// Maximum number of tokens to generate.
    /// Range: 1-128,000 (actual max depends on selected model).
    /// </summary>
    [JsonPropertyName("maxOutputTokens")]
    [ConfigurableField(Label = "Max Output Tokens", ControlType = ControlType.Slider, Order = 30)]
    [Tab("Basic")]
    [Slider(Min = 1, Max = 128000, Step = 256, Default = 4096)]
    public int? MaxOutputTokens { get; init; }

    /// <summary>
    /// System prompts that define the assistant's behavior.
    /// </summary>
    [JsonPropertyName("systemPrompts")]
    [ConfigurableField(Label = "System Prompts", ControlType = ControlType.TextAreaList, Order = 10)]
    [Tab("Prompts", Order = 2, Icon = "message-square")]
    [SupportVariables]
    public List<string>? SystemPrompts { get; init; }

    /// <summary>
    /// User messages to send to the model.
    /// </summary>
    [JsonPropertyName("userMessages")]
    [ConfigurableField(Label = "User Messages", ControlType = ControlType.TextAreaList, Order = 20, Required = true)]
    [Tab("Prompts")]
    [SupportVariables]
    public required List<string> UserMessages { get; init; }

    /// <summary>
    /// Provider-specific configuration (polymorphic).
    /// </summary>
    [JsonPropertyName("providerConfig")]
    public ProviderConfig? ProviderConfig { get; init; }
}
