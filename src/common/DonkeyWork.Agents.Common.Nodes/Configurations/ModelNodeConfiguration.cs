using System.Text.Json.Serialization;
using DonkeyWork.Agents.Common.Contracts.Enums;
using DonkeyWork.Agents.Common.Nodes.Attributes;
using DonkeyWork.Agents.Common.Nodes.Enums;

namespace DonkeyWork.Agents.Common.Nodes.Configurations;

/// <summary>
/// Configuration for the Model node - calls an LLM with configured prompts and parameters.
/// </summary>
public sealed class ModelNodeConfiguration : NodeConfiguration, IRequiresCredential
{
    /// <inheritdoc />
    public override NodeType Type => NodeType.Model;

    /// <summary>
    /// The LLM provider to use.
    /// </summary>
    [JsonPropertyName("provider")]
    [ConfigurableField(Label = "Provider", ControlType = ControlType.Select, Order = 10)]
    [Tab("Basic", Order = 1, Icon = "settings")]
    public required LlmProvider Provider { get; init; }

    /// <summary>
    /// The model ID to use (e.g., "gpt-4", "claude-3-opus").
    /// </summary>
    [JsonPropertyName("modelId")]
    [ConfigurableField(Label = "Model", ControlType = ControlType.Select, Order = 20)]
    [Tab("Basic", Order = 1)]
    public required string ModelId { get; init; }

    /// <summary>
    /// The credential ID for authenticating with the LLM provider.
    /// </summary>
    [JsonPropertyName("credentialId")]
    [ConfigurableField(Label = "Credential", ControlType = ControlType.Credential, Order = 30)]
    [Tab("Basic", Order = 1)]
    public required Guid CredentialId { get; init; }

    /// <summary>
    /// System prompts that define the assistant's behavior.
    /// </summary>
    [JsonPropertyName("systemPrompts")]
    [ConfigurableField(Label = "System Prompts", ControlType = ControlType.TextAreaList, Order = 10)]
    [Tab("Prompts", Order = 2, Icon = "brain")]
    [SupportVariables]
    public List<string>? SystemPrompts { get; init; }

    /// <summary>
    /// User messages to send to the model.
    /// </summary>
    [JsonPropertyName("userMessages")]
    [ConfigurableField(Label = "User Messages", ControlType = ControlType.TextAreaList, Order = 20, Required = true)]
    [Tab("Prompts", Order = 2)]
    [SupportVariables]
    public required List<string> UserMessages { get; init; }

    /// <summary>
    /// Sampling temperature (0-2). Higher values make output more random.
    /// </summary>
    [JsonPropertyName("temperature")]
    [ConfigurableField(Label = "Temperature", ControlType = ControlType.Slider, Order = 10)]
    [Tab("Advanced", Order = 3, Icon = "sliders")]
    [Slider(Min = 0, Max = 2, Step = 0.1, Default = 1.0)]
    public double? Temperature { get; init; }

    /// <summary>
    /// Maximum number of tokens to generate.
    /// </summary>
    [JsonPropertyName("maxOutputTokens")]
    [ConfigurableField(Label = "Max Output Tokens", ControlType = ControlType.Number, Order = 20)]
    [Tab("Advanced", Order = 3)]
    public int? MaxOutputTokens { get; init; }

    /// <summary>
    /// Top-p (nucleus) sampling parameter (0-1).
    /// </summary>
    [JsonPropertyName("topP")]
    [ConfigurableField(Label = "Top P", ControlType = ControlType.Slider, Order = 30)]
    [Tab("Advanced", Order = 3)]
    [Slider(Min = 0, Max = 1, Step = 0.05, Default = 1.0)]
    public double? TopP { get; init; }
}
